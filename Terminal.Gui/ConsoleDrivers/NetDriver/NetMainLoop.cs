using System.Collections.Concurrent;

namespace Terminal.Gui;

/// <summary>
///     Mainloop intended to be used with the .NET System.Console API, and can be used on Windows and Unix, it is
///     cross-platform but lacks things like file descriptor monitoring.
/// </summary>
/// <remarks>This implementation is used for NetDriver.</remarks>
internal class NetMainLoop : IMainLoopDriver
{
    internal NetEvents _netEvents;

    /// <summary>Invoked when a Key is pressed.</summary>
    internal Action<NetEvents.InputResult> ProcessInput;

    private readonly CancellationTokenSource _inputHandlerTokenSource = new ();
    private readonly BlockingCollection<NetEvents.InputResult> _resultQueue = new (new ConcurrentQueue<NetEvents.InputResult> ());
    internal readonly ManualResetEventSlim _waitForProbe = new (false);
    private readonly CancellationTokenSource _eventReadyTokenSource = new ();
    private MainLoop _mainLoop;

    /// <summary>Initializes the class with the console driver.</summary>
    /// <remarks>Passing a consoleDriver is provided to capture windows resizing.</remarks>
    /// <param name="consoleDriver">The console driver used by this Net main loop.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public NetMainLoop (ConsoleDriver consoleDriver = null)
    {
        if (consoleDriver is null)
        {
            throw new ArgumentNullException (nameof (consoleDriver));
        }

        _netEvents = new NetEvents (consoleDriver);
    }

    void IMainLoopDriver.Setup (MainLoop mainLoop)
    {
        _mainLoop = mainLoop;

        if (ConsoleDriver.RunningUnitTests)
        {
            return;
        }

        Task.Run (NetInputHandler, _inputHandlerTokenSource.Token);
    }

    void IMainLoopDriver.Wakeup () { }

    bool IMainLoopDriver.EventsPending ()
    {
        _waitForProbe.Set ();

        if (_mainLoop.CheckTimersAndIdleHandlers (out int _))
        {
            return true;
        }

        _eventReadyTokenSource.Token.ThrowIfCancellationRequested ();

        if (!_eventReadyTokenSource.IsCancellationRequested)
        {
            return _resultQueue.Count > 0 || _mainLoop.CheckTimersAndIdleHandlers (out _);
        }

        return _resultQueue.Count > 0;
    }

    void IMainLoopDriver.Iteration ()
    {
        while (_resultQueue.Count > 0)
        {
            // Always dequeue even if it's null and invoke if isn't null
            if (_resultQueue.TryTake (out NetEvents.InputResult dequeueResult))
            {
                if (dequeueResult is { })
                {
                    ProcessInput?.Invoke (dequeueResult);
                }
            }
        }
    }

    void IMainLoopDriver.TearDown ()
    {
        _inputHandlerTokenSource?.Cancel ();
        _inputHandlerTokenSource?.Dispose ();
        _eventReadyTokenSource?.Cancel ();
        _eventReadyTokenSource?.Dispose ();

        _waitForProbe?.Dispose ();
        _netEvents?.Dispose ();
        _netEvents = null;

        _mainLoop = null;
    }

    private void NetInputHandler ()
    {
        while (_mainLoop is { })
        {
            try
            {
                if (!_netEvents._forceRead && !_inputHandlerTokenSource.IsCancellationRequested)
                {
                    _waitForProbe.Wait (_inputHandlerTokenSource.Token);
                }
            }
            catch (OperationCanceledException)
            {
                return;
            }
            finally
            {
                if (_waitForProbe.IsSet)
                {
                    _waitForProbe.Reset ();
                }
            }

            if (_inputHandlerTokenSource.IsCancellationRequested)
            {
                return;
            }

            _inputHandlerTokenSource.Token.ThrowIfCancellationRequested ();

            if (_resultQueue.Count == 0)
            {
                var result = _netEvents.DequeueInput ();

                if (result.HasValue)
                {
                    _resultQueue.Add (result.Value);
                }
            }
        }
    }
}
