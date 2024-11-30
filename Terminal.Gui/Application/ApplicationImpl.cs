using System.Diagnostics.CodeAnalysis;

namespace Terminal.Gui;


internal class ApplicationImpl : IApplication
{

    /// <summary>Gets the <see cref="IConsoleDriver"/> that has been selected.</summary>
    public IConsoleDriver? Driver { get; internal set; }

    // Private static readonly Lazy instance of Application
    private static readonly Lazy<IApplication> lazyInstance =
        new Lazy<IApplication> (() => new ApplicationImpl ());

    // Public static property to access the instance
    public static IApplication Instance => lazyInstance.Value;

    public bool Initialized { get; set; }

    // Private constructor to prevent external instantiation
    protected ApplicationImpl ()
    {
    }

    // INTERNAL function for initializing an app with a Toplevel factory object, driver, and mainloop.
    //
    // Called from:
    //
    // Init() - When the user wants to use the default Toplevel. calledViaRunT will be false, causing all state to be reset.
    // Run<T>() - When the user wants to use a custom Toplevel. calledViaRunT will be true, enabling Run<T>() to be called without calling Init first.
    // Unit Tests - To initialize the app with a custom Toplevel, using the FakeDriver. calledViaRunT will be false, causing all state to be reset.
    //
    // calledViaRunT: If false (default) all state will be reset. If true the state will not be reset.
    [RequiresUnreferencedCode ("AOT")]
    [RequiresDynamicCode ("AOT")]
    internal void InternalInit (
        IConsoleDriver? driver = null,
        string? driverName = null,
        bool calledViaRunT = false
    )
    {
        if (Initialized && driver is null)
        {
            return;
        }

        if (Initialized)
        {
            throw new InvalidOperationException ("Init has already been called and must be bracketed by Shutdown.");
        }

        if (!calledViaRunT)
        {
            // Reset all class variables (Application is a singleton).
            ResetState (ignoreDisposed: true);
        }

        Navigation = new ();

        // For UnitTests
        if (driver is { })
        {
            Driver = driver;

            if (driver is FakeDriver)
            {
                // We're running unit tests. Disable loading config files other than default
                if (Locations == ConfigLocations.All)
                {
                    Locations = ConfigLocations.Default;
                    Reset ();
                }
            }
        }

        AddApplicationKeyBindings ();

        // Start the process of configuration management.
        // Note that we end up calling LoadConfigurationFromAllSources
        // multiple times. We need to do this because some settings are only
        // valid after a Driver is loaded. In this case we need just
        // `Settings` so we can determine which driver to use.
        // Don't reset, so we can inherit the theme from the previous run.
        string previousTheme = Themes?.Theme ?? string.Empty;
        Load ();
        if (Themes is { } && !string.IsNullOrEmpty (previousTheme) && previousTheme != "Default")
        {
            ThemeManager.SelectedTheme = previousTheme;
        }
        Apply ();

        // Ignore Configuration for ForceDriver if driverName is specified
        if (!string.IsNullOrEmpty (driverName))
        {
            ForceDriver = driverName;
        }

        if (Driver is null)
        {
            PlatformID p = Environment.OSVersion.Platform;

            if (string.IsNullOrEmpty (ForceDriver))
            {
                if (p == PlatformID.Win32NT || p == PlatformID.Win32S || p == PlatformID.Win32Windows)
                {
                    Driver = new WindowsDriver ();
                }
                else
                {
                    Driver = new CursesDriver ();
                }
            }
            else
            {
                List<Type?> drivers = GetDriverTypes ();
                Type? driverType = drivers.FirstOrDefault (t => t!.Name.Equals (ForceDriver, StringComparison.InvariantCultureIgnoreCase));

                if (driverType is { })
                {
                    Driver = (IConsoleDriver)Activator.CreateInstance (driverType)!;
                }
                else
                {
                    throw new ArgumentException (
                                                 $"Invalid driver name: {ForceDriver}. Valid names are {string.Join (", ", drivers.Select (t => t!.Name))}"
                                                );
                }
            }
        }

        try
        {
            MainLoop = Driver!.Init ();
        }
        catch (InvalidOperationException ex)
        {
            // This is a case where the driver is unable to initialize the console.
            // This can happen if the console is already in use by another process or
            // if running in unit tests.
            // In this case, we want to throw a more specific exception.
            throw new InvalidOperationException (
                                                 "Unable to initialize the console. This can happen if the console is already in use by another process or in unit tests.",
                                                 ex
                                                );
        }

        Driver.SizeChanged += Driver_SizeChanged;
        Driver.KeyDown += Driver_KeyDown;
        Driver.KeyUp += Driver_KeyUp;
        Driver.MouseEvent += Driver_MouseEvent;

        SynchronizationContext.SetSynchronizationContext (new MainLoopSyncContext ());

        SupportedCultures = GetSupportedCultures ();
        MainThreadId = Thread.CurrentThread.ManagedThreadId;
        bool init = Initialized = true;
        InitializedChanged?.Invoke (null, new (init));
    }
}
