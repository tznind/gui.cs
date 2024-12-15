using System.Collections.ObjectModel;

namespace Terminal.Gui;

public class TimedEvents : ITimedEvents
{
    internal List<Func<bool>> _idleHandlers = new ();
    internal SortedList<long, Timeout> _timeouts = new ();

    /// <summary>The idle handlers and lock that must be held while manipulating them</summary>
    private readonly object _idleHandlersLock = new ();

    private readonly object _timeoutsLockToken = new ();


    /// <summary>Gets a copy of the list of all idle handlers.</summary>
    public ReadOnlyCollection<Func<bool>> IdleHandlers
    {
        get
        {
            lock (_idleHandlersLock)
            {
                return new List<Func<bool>> (_idleHandlers).AsReadOnly ();
            }
        }
    }

    /// <summary>
    ///     Gets the list of all timeouts sorted by the <see cref="TimeSpan"/> time ticks. A shorter limit time can be
    ///     added at the end, but it will be called before an earlier addition that has a longer limit time.
    /// </summary>
    public SortedList<long, Timeout> Timeouts => _timeouts;

    /// <inheritdoc />
    public void AddIdle (Func<bool> idleHandler)
    {
        lock (_idleHandlersLock)
        {
            _idleHandlers.Add (idleHandler);
        }
    }

    /// <summary>
    ///     Invoked when a new timeout is added. To be used in the case when
    ///     <see cref="Application.EndAfterFirstIteration"/> is <see langword="true"/>.
    /// </summary>
    public event EventHandler<TimeoutEventArgs>? TimeoutAdded;


    private void AddTimeout (TimeSpan time, Timeout timeout)
    {
        lock (_timeoutsLockToken)
        {
            long k = (DateTime.UtcNow + time).Ticks;
            _timeouts.Add (NudgeToUniqueKey (k), timeout);
            TimeoutAdded?.Invoke (this, new TimeoutEventArgs (timeout, k));
        }
    }

    /// <summary>
    ///     Finds the closest number to <paramref name="k"/> that is not present in <see cref="_timeouts"/>
    ///     (incrementally).
    /// </summary>
    /// <param name="k"></param>
    /// <returns></returns>
    private long NudgeToUniqueKey (long k)
    {
        lock (_timeoutsLockToken)
        {
            while (_timeouts.ContainsKey (k))
            {
                k++;
            }
        }

        return k;
    }


    // PERF: This is heavier than it looks.
    // CONCURRENCY: Potential deadlock city here.
    // CONCURRENCY: Multiple concurrency pitfalls on the delegates themselves.
    // INTENT: It looks like the general architecture here is trying to be a form of publisher/consumer pattern.
    private void RunIdle ()
    {
        Func<bool> [] iterate;
        lock (_idleHandlers)
        {
            iterate = _idleHandlers.ToArray ();
            _idleHandlers = new List<Func<bool>> ();
        }

        foreach (Func<bool> idle in iterate)
        {
            if (idle ())
            {
                lock (_idleHandlers)
                {
                    _idleHandlers.Add (idle);
                }
            }
        }
    }


    public void LockAndRunTimers ()
    {
        lock (_timeoutsLockToken)
        {
            if (_timeouts.Count > 0)
            {
                RunTimers ();
            }
        }

    }

    public void LockAndRunIdles ()
    {
        bool runIdle;

        lock (_idleHandlersLock)
        {
            runIdle = _idleHandlers.Count > 0;
        }

        if (runIdle)
        {
            RunIdle ();
        }
    }
    private void RunTimers ()
    {
        long now = DateTime.UtcNow.Ticks;
        SortedList<long, Timeout> copy;

        // lock prevents new timeouts being added
        // after we have taken the copy but before
        // we have allocated a new list (which would
        // result in lost timeouts or errors during enumeration)
        lock (_timeoutsLockToken)
        {
            copy = _timeouts;
            _timeouts = new SortedList<long, Timeout> ();
        }

        foreach ((long k, Timeout timeout) in copy)
        {
            if (k < now)
            {
                if (timeout.Callback ())
                {
                    AddTimeout (timeout.Span, timeout);
                }
            }
            else
            {
                lock (_timeoutsLockToken)
                {
                    _timeouts.Add (NudgeToUniqueKey (k), timeout);
                }
            }
        }
    }


    /// <summary>Removes an idle handler added with <see cref="AddIdle(Func{bool})"/> from processing.</summary>
    /// <param name="token">A token returned by <see cref="AddIdle(Func{bool})"/></param>
    /// Returns
    /// <c>true</c>
    /// if the idle handler is successfully removed; otherwise,
    /// <c>false</c>
    /// .
    /// This method also returns
    /// <c>false</c>
    /// if the idle handler is not found.
    public bool RemoveIdle (Func<bool> token)
    {
        lock (_idleHandlersLock)
        {
            return _idleHandlers.Remove (token);
        }
    }

    /// <summary>Removes a previously scheduled timeout</summary>
    /// <remarks>The token parameter is the value returned by AddTimeout.</remarks>
    /// Returns
    /// <c>true</c>
    /// if the timeout is successfully removed; otherwise,
    /// <c>false</c>
    /// .
    /// This method also returns
    /// <c>false</c>
    /// if the timeout is not found.
    public bool RemoveTimeout (object token)
    {
        lock (_timeoutsLockToken)
        {
            int idx = _timeouts.IndexOfValue ((token as Timeout)!);

            if (idx == -1)
            {
                return false;
            }

            _timeouts.RemoveAt (idx);
        }

        return true;
    }


    /// <summary>Adds a timeout to the <see cref="MainLoop"/>.</summary>
    /// <remarks>
    ///     When time specified passes, the callback will be invoked. If the callback returns true, the timeout will be
    ///     reset, repeating the invocation. If it returns false, the timeout will stop and be removed. The returned value is a
    ///     token that can be used to stop the timeout by calling <see cref="RemoveTimeout(object)"/>.
    /// </remarks>
    public object AddTimeout (TimeSpan time, Func<bool> callback)
    {
        ArgumentNullException.ThrowIfNull (callback);

        var timeout = new Timeout { Span = time, Callback = callback };
        AddTimeout (time, timeout);

        return timeout;
    }

    /// <summary>
    ///     Called from <see cref="IMainLoopDriver.EventsPending"/> to check if there are any outstanding timers or idle
    ///     handlers.
    /// </summary>
    /// <param name="waitTimeout">
    ///     Returns the number of milliseconds remaining in the current timer (if any). Will be -1 if
    ///     there are no active timers.
    /// </param>
    /// <returns><see langword="true"/> if there is a timer or idle handler active.</returns>
    public bool CheckTimersAndIdleHandlers (out int waitTimeout)
    {
        long now = DateTime.UtcNow.Ticks;

        waitTimeout = 0;

        lock (_timeoutsLockToken)
        {
            if (_timeouts.Count > 0)
            {
                waitTimeout = (int)((_timeouts.Keys [0] - now) / TimeSpan.TicksPerMillisecond);

                if (waitTimeout < 0)
                {
                    // This avoids 'poll' waiting infinitely if 'waitTimeout < 0' until some action is detected
                    // This can occur after IMainLoopDriver.Wakeup is executed where the pollTimeout is less than 0
                    // and no event occurred in elapsed time when the 'poll' is start running again.
                    waitTimeout = 0;
                }

                return true;
            }

            // ManualResetEventSlim.Wait, which is called by IMainLoopDriver.EventsPending, will wait indefinitely if
            // the timeout is -1.
            waitTimeout = -1;
        }

        // There are no timers set, check if there are any idle handlers

        lock (_idleHandlers)
        {
            return _idleHandlers.Count > 0;
        }
    }
}

public interface ITimedEvents
{
    void AddIdle (Func<bool> idleHandler);
    void LockAndRunIdles ();
    void LockAndRunTimers ();
    bool CheckTimersAndIdleHandlers (out int waitTimeout);

    /// <summary>Adds a timeout to the application.</summary>
    /// <remarks>
    ///     When time specified passes, the callback will be invoked. If the callback returns true, the timeout will be
    ///     reset, repeating the invocation. If it returns false, the timeout will stop and be removed. The returned value is a
    ///     token that can be used to stop the timeout by calling <see cref="RemoveTimeout(object)"/>.
    /// </remarks>
    object AddTimeout (TimeSpan time, Func<bool> callback);

    /// <summary>Removes a previously scheduled timeout</summary>
    /// <remarks>The token parameter is the value returned by AddTimeout.</remarks>
    /// Returns
    /// <c>true</c>
    /// if the timeout is successfully removed; otherwise,
    /// <c>false</c>
    /// .
    /// This method also returns
    /// <c>false</c>
    /// if the timeout is not found.
    bool RemoveTimeout (object token);

    ReadOnlyCollection<Func<bool>> IdleHandlers { get;}

    SortedList<long, Timeout> Timeouts { get; }
    bool RemoveIdle (Func<bool> fnTrue);

    event EventHandler<TimeoutEventArgs>? TimeoutAdded;
}
