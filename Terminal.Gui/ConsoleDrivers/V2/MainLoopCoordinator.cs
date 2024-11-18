namespace Terminal.Gui.ConsoleDrivers.V2;
class MainLoopCoordinator<T>
{
    private readonly IConsoleInput<T> _input;
    private readonly IMainLoop<T> _loop;
    private CancellationTokenSource tokenSource = new CancellationTokenSource ();

    public MainLoopCoordinator (IConsoleInput<T> input, IMainLoop<T> loop)
    {
        _input = input;
        _loop = loop;
    }
    public void Start ()
    {
        Task.Run (RunInput);
        Task.Run (RunLoop);
    }

    private void RunInput ()
    {
        try
        {
            _input.Run (tokenSource.Token);
        }
        catch (OperationCanceledException)
        {
        }
        _input.Dispose ();
    }

    private void RunLoop ()
    {
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
