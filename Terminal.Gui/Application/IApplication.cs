namespace Terminal.Gui;

public interface IApplication
{
    public IConsoleDriver Driver { get; set; }

    /// <summary>
    ///     Gets whether the application has been initialized with <see cref="Init"/> and not yet shutdown with <see cref="Shutdown"/>.
    /// </summary>
    public bool Initialized { get; set; }


    /// <summary>
    ///     Gets the <see cref="ApplicationNavigation"/> instance for the current <see cref="Application"/>.
    /// </summary>
    ApplicationNavigation Navigation { get; set; }

    /// <summary>Gets the Application-scoped key bindings.</summary>
    KeyBindings KeyBindings { get; set; }

    public void Shutdown ();
    public void RequestStop ();
    public void Init ();

}
