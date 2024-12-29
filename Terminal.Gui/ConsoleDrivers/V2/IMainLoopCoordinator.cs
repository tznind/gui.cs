namespace Terminal.Gui;

/// <summary>
/// Interface for main Terminal.Gui loop manager in v2.
/// </summary>
public interface IMainLoopCoordinator
{
    /// <summary>
    /// Create all required subcomponents and boot strap.
    /// </summary>
    /// <returns></returns>
    public Task StartAsync ();

    /// <summary>
    /// Stop and dispose all subcomponents
    /// </summary>
    public void Stop ();

    /// <summary>
    /// Run a single iteration of the main UI loop
    /// </summary>
    void RunIteration ();
}
