using System.Collections.Concurrent;

namespace Terminal.Gui;

public class MainLoopCoordinator<T> : IMainLoopCoordinator
{
    private readonly Func<IConsoleInput<T>> _inputFactory;
    private readonly ConcurrentQueue<T> _inputBuffer;
    private readonly IInputProcessor _inputProcessor;
    private readonly IMainLoop<T> _loop;
    private CancellationTokenSource tokenSource = new ();
    private readonly Func<IConsoleOutput> _outputFactory;

    /// <summary>
    /// Creates a new coordinator
    /// </summary>
    /// <param name="inputFactory">Function to create a new input. This must call <see langword="new"/>
    ///     explicitly and cannot return an existing instance. This requirement arises because Windows
    ///     console screen buffer APIs are thread-specific for certain operations.</param>
    /// <param name="inputBuffer"></param>
    /// <param name="inputProcessor"></param>
    /// <param name="outputFactory">Function to create a new output. This must call <see langword="new"/>
    ///     explicitly and cannot return an existing instance. This requirement arises because Windows
    ///     console screen buffer APIs are thread-specific for certain operations.</param>
    /// <param name="loop"></param>
    public MainLoopCoordinator (Func<IConsoleInput<T>> inputFactory, ConcurrentQueue<T> inputBuffer,IInputProcessor inputProcessor, Func<IConsoleOutput> outputFactory, IMainLoop<T> loop)
    {
        _inputFactory = inputFactory;
        _inputBuffer = inputBuffer;
        _inputProcessor = inputProcessor;
        _outputFactory = outputFactory;
        _loop = loop;
    }

    /// <summary>
    /// Starts the main and input loop threads in separate tasks (returning immediately).
    /// </summary>
    public void StartAsync ()
    {
        Task.Run (RunInput);
        Task.Run (RunLoop);
    }
    /// <summary>
    /// Starts the input thread and then enters the main loop in the current thread
    /// (method only exits when application ends).
    /// </summary>
    public void StartBlocking ()
    {
        Task.Run (RunInput);
        RunLoop();
    }
    private void RunInput ()
    {
        // Instance must be constructed on the thread in which it is used.
        IConsoleInput<T> input = _inputFactory.Invoke ();
        input.Initialize (_inputBuffer);
        try
        {
            input.Run (tokenSource.Token);
        }
        catch (OperationCanceledException)
        {
        }
        input.Dispose ();
    }

    private void RunLoop ()
    {
        // Instance must be constructed on the thread in which it is used.
        IConsoleOutput output = _outputFactory.Invoke ();

        _loop.Initialize (_inputBuffer, _inputProcessor, output);

        try
        {
            _loop.Run (tokenSource.Token);
        }
        catch (OperationCanceledException)
        {
        }
        _loop.Dispose ();
    }

    public void Stop ()
    {
        tokenSource.Cancel();
    }
}

public interface IMainLoopCoordinator
{
    public void StartAsync ();
    public void StartBlocking ();

    public void Stop ();
}
