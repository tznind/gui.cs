namespace Terminal.Gui;

public interface IMainLoopCoordinator
{
    public Task StartAsync ();

    public void Stop ();

    public Exception InputCrashedException { get; }
    void RunIteration ();
}
