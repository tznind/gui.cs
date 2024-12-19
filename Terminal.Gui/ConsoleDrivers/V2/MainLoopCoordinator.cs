using System.Collections.Concurrent;
using Terminal.Gui.ConsoleDrivers.V2;

namespace Terminal.Gui;

public class MainLoopCoordinator<T> : IMainLoopCoordinator
{
    private readonly Func<IConsoleInput<T>> _inputFactory;
    private readonly ConcurrentQueue<T> _inputBuffer;
    private readonly IInputProcessor _inputProcessor;
    private readonly IMainLoop<T> _loop;
    private CancellationTokenSource tokenSource = new ();
    private readonly Func<IConsoleOutput> _outputFactory;
    private IConsoleInput<T> _input;
    private IConsoleOutput _output;
    object oLockInitialization = new ();
    private ConsoleDriverFacade<T> _facade;
    private Task _inputTask;
    private Task _loopTask;
    private ITimedEvents _timedEvents;

    public Exception InputCrashedException { get; private set; }
    public Exception LoopCrashedException { get; private set; }

    public SemaphoreSlim StartupSemaphore { get; } = new (0, 1);

    /// <summary>
    /// Creates a new coordinator
    /// </summary>
    /// <param name="timedEvents"></param>
    /// <param name="inputFactory">Function to create a new input. This must call <see langword="new"/>
    ///     explicitly and cannot return an existing instance. This requirement arises because Windows
    ///     console screen buffer APIs are thread-specific for certain operations.</param>
    /// <param name="inputBuffer"></param>
    /// <param name="inputProcessor"></param>
    /// <param name="outputFactory">Function to create a new output. This must call <see langword="new"/>
    ///     explicitly and cannot return an existing instance. This requirement arises because Windows
    ///     console screen buffer APIs are thread-specific for certain operations.</param>
    /// <param name="loop"></param>
    public MainLoopCoordinator (ITimedEvents timedEvents, Func<IConsoleInput<T>> inputFactory, ConcurrentQueue<T> inputBuffer,IInputProcessor inputProcessor, Func<IConsoleOutput> outputFactory, IMainLoop<T> loop)
    {
        _timedEvents = timedEvents;
        _inputFactory = inputFactory;
        _inputBuffer = inputBuffer;
        _inputProcessor = inputProcessor;
        _outputFactory = outputFactory;
        _loop = loop;
    }

    /// <summary>
    /// Starts the main and input loop threads in separate tasks (returning immediately).
    /// </summary>
    public async Task StartAsync ()
    {
        // TODO: if crash on boot then semaphore never finishes
        _inputTask = Task.Run (RunInput);
        _loopTask = Task.Run (RunLoop);

        // Use asynchronous semaphore waiting.
        await StartupSemaphore.WaitAsync ().ConfigureAwait (false);
    }

    private void RunInput ()
    {
        try
        {
            lock (oLockInitialization)
            {
                // Instance must be constructed on the thread in which it is used.
                _input = _inputFactory.Invoke ();
                _input.Initialize (_inputBuffer);

                BuildFacadeIfPossible ();
            }

            try
            {
                _input.Run (tokenSource.Token);
            }
            catch (OperationCanceledException)
            {
            }
            _input.Dispose ();
        }
        catch (Exception e)
        {
            InputCrashedException = e;
        }
    }

    private void RunLoop ()
    {
        try
        {
            lock (oLockInitialization)
            {
                // Instance must be constructed on the thread in which it is used.
                _output = _outputFactory.Invoke ();
                _loop.Initialize (_timedEvents, _inputBuffer, _inputProcessor, _output);

                BuildFacadeIfPossible ();
            }

            try
            {
                _loop.Run (tokenSource.Token);
            }
            catch (OperationCanceledException)
            {
            }
            _loop.Dispose ();
        }
        catch (Exception e)
        {
            LoopCrashedException = e;
        }
    }

    private void BuildFacadeIfPossible ()
    {
        if (_input != null && _output != null)
        {
            _facade = new ConsoleDriverFacade<T> (
                                                  _inputProcessor,
                                                  _loop.OutputBuffer,
                                                  _output,
                                                  _loop.AnsiRequestScheduler,
                                                  _loop.WindowSizeMonitor);
            Application.Driver = _facade;

            StartupSemaphore.Release ();
        }
    }

    public void Stop ()
    {
        tokenSource.Cancel();

        if (_loopTask.Id == Task.CurrentId)
        {
            // Cannot wait for loop to finish because we are actively executing on that thread
            Task.WhenAll (_inputTask).Wait ();
        }
        else
        {
            // Wait for both tasks to complete
            Task.WhenAll (_inputTask, _loopTask).Wait ();
        }

    }
}