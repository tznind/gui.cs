#nullable enable
using System.Collections.ObjectModel;

namespace Terminal.Gui;

public interface ITimedEvents
{
    void AddIdle (Func<bool> idleHandler);
    void LockAndRunIdles ();
    void LockAndRunTimers ();

    /// <summary>
    ///     Called from <see cref="IMainLoopDriver.EventsPending"/> to check if there are any outstanding timers or idle
    ///     handlers.
    /// </summary>
    /// <param name="waitTimeout">
    ///     Returns the number of milliseconds remaining in the current timer (if any). Will be -1 if
    ///     there are no active timers.
    /// </param>
    /// <returns><see langword="true"/> if there is a timer or idle handler active.</returns>
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
    /// <returns>
    /// Returns
    /// <see langword="true"/>
    /// if the timeout is successfully removed; otherwise,
    /// <see langword="false"/>
    /// .
    /// This method also returns
    /// <see langword="false"/>
    /// if the timeout is not found.
    /// </returns>
    bool RemoveTimeout (object token);

    ReadOnlyCollection<Func<bool>> IdleHandlers { get;}

    SortedList<long, Timeout> Timeouts { get; }


    /// <summary>Removes an idle handler added with <see cref="AddIdle(Func{bool})"/> from processing.</summary>
    /// <returns>
    /// <see langword="true"/>
    /// if the idle handler is successfully removed; otherwise,
    /// <see langword="false"/>
    /// .
    /// This method also returns
    /// <see langword="false"/>
    /// if the idle handler is not found.</returns>
    bool RemoveIdle (Func<bool> fnTrue);

    /// <summary>
    ///     Invoked when a new timeout is added. To be used in the case when
    ///     <see cref="Application.EndAfterFirstIteration"/> is <see langword="true"/>.
    /// </summary>
    event EventHandler<TimeoutEventArgs>? TimeoutAdded;
}
