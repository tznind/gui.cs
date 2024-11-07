﻿using System.Collections.Concurrent;

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

    private readonly ManualResetEventSlim _eventReady = new (false);
    private readonly CancellationTokenSource _inputHandlerTokenSource = new ();
    private readonly ConcurrentQueue<NetEvents.InputResult> _resultQueue = new ();
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

    void IMainLoopDriver.Wakeup () { _eventReady.Set (); }

    bool IMainLoopDriver.EventsPending ()
    {
        _waitForProbe.Set ();

        if (_mainLoop.CheckTimersAndIdleHandlers (out int waitTimeout))
        {
            return true;
        }

        try
        {
            if (!_eventReadyTokenSource.IsCancellationRequested)
            {
                // Note: ManualResetEventSlim.Wait will wait indefinitely if the timeout is -1. The timeout is -1 when there
                // are no timers, but there IS an idle handler waiting.
                _eventReady.Wait (waitTimeout, _eventReadyTokenSource.Token);
            }
        }
        catch (OperationCanceledException)
        {
            return true;
        }
        finally
        {
            _eventReady.Reset ();
        }

        _eventReadyTokenSource.Token.ThrowIfCancellationRequested ();

        if (!_eventReadyTokenSource.IsCancellationRequested)
        {
            return _resultQueue.Count > 0 || _mainLoop.CheckTimersAndIdleHandlers (out _);
        }

        return true;
    }

    void IMainLoopDriver.Iteration ()
    {
        while (_resultQueue.Count > 0)
        {
            // Always dequeue even if it's null and invoke if isn't null
            if (_resultQueue.TryDequeue (out NetEvents.InputResult dequeueResult))
            {
                ProcessInput?.Invoke (dequeueResult);
            }
        }
    }

    void IMainLoopDriver.TearDown ()
    {
        _inputHandlerTokenSource?.Cancel ();
        _inputHandlerTokenSource?.Dispose ();
        _eventReadyTokenSource?.Cancel ();
        _eventReadyTokenSource?.Dispose ();

        _eventReady?.Dispose ();

        _resultQueue?.Clear ();
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
                    _resultQueue.Enqueue (result.Value);
                }
            }

            if (_resultQueue.Count > 0)
            {
                _eventReady.Set ();
            }
        }
    }
}
