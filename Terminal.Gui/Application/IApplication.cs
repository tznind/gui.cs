namespace Terminal.Gui;

public interface IApplication
{
    public IConsoleDriver Driver { get; set; }

    /// <summary>
    ///     Gets whether the application has been initialized with <see cref="Init"/> and not yet shutdown with <see cref="Shutdown"/>.
    /// </summary>
    public bool Initialized { get; set; }


    public void Shutdown ();
    public void RequestStop ();
    public void Init ();

}
