namespace Terminal.Gui;

public interface IMainLoopCoordinator
{
    /// <summary>
    /// Can be waited on after calling <see cref="StartAsync"/> to know when
    /// boot up threads are running. Do not wait unless you constructed the coordinator.
    /// Do not wait multiple times on the same coordinator.
    /// </summary>
    SemaphoreSlim StartupSemaphore { get; }
    public void StartAsync ();
    public void StartBlocking ();

    public void Stop ();
}
