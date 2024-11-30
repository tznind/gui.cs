using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using static Terminal.Gui.View;
using static Unix.Terminal.Curses;

namespace Terminal.Gui;


internal class ApplicationImpl : IApplication
{
    /// <summary>Gets the <see cref="IConsoleDriver"/> that has been selected.</summary>
    public IConsoleDriver? Driver { get; set; }

    // Private static readonly Lazy instance of Application
    private static readonly Lazy<IApplication> lazyInstance =
        new Lazy<IApplication> (() => new ApplicationImpl ());

    // Public static property to access the instance
    public static IApplication Instance => lazyInstance.Value;

    public bool Initialized { get; set; }
    public ApplicationNavigation Navigation { get; set; }

    /// <inheritdoc />
    public KeyBindings KeyBindings { get; set; } = new ();

    // Private constructor to prevent external instantiation
    protected ApplicationImpl ()
    {
        AddApplicationKeyBindings ();
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

    internal void AddApplicationKeyBindings ()
    {
        CommandImplementations = new ();

        // Things this view knows how to do
        AddCommand (
                    Command.Quit,
                    static () =>
                    {
                        RequestStop ();

                        return true;
                    }
                   );

        AddCommand (
                    Command.Suspend,
                    static () =>
                    {
                        Driver?.Suspend ();

                        return true;
                    }
                   );

        AddCommand (
                    Command.NextTabStop,
                    static () => Navigation?.AdvanceFocus (NavigationDirection.Forward, TabBehavior.TabStop));

        AddCommand (
                    Command.PreviousTabStop,
                    static () => Navigation?.AdvanceFocus (NavigationDirection.Backward, TabBehavior.TabStop));

        AddCommand (
                    Command.NextTabGroup,
                    static () => Navigation?.AdvanceFocus (NavigationDirection.Forward, TabBehavior.TabGroup));

        AddCommand (
                    Command.PreviousTabGroup,
                    static () => Navigation?.AdvanceFocus (NavigationDirection.Backward, TabBehavior.TabGroup));

        AddCommand (
                    Command.Refresh,
                    static () =>
                    {
                        LayoutAndDraw (true);

                        return true;
                    }
                   );

        AddCommand (
                    Command.Edit,
                    static () =>
                    {
                        View? viewToArrange = Navigation?.GetFocused ();

                        // Go up the superview hierarchy and find the first that is not ViewArrangement.Fixed
                        while (viewToArrange is { SuperView: { }, Arrangement: ViewArrangement.Fixed })
                        {
                            viewToArrange = viewToArrange.SuperView;
                        }

                        if (viewToArrange is { })
                        {
                            return viewToArrange.Border?.EnterArrangeMode (ViewArrangement.Fixed);
                        }

                        return false;
                    });

        KeyBindings.Clear ();

        // Resources/config.json overrides
        NextTabKey = Key.Tab;
        PrevTabKey = Key.Tab.WithShift;
        NextTabGroupKey = Key.F6;
        PrevTabGroupKey = Key.F6.WithShift;
        QuitKey = Key.Esc;
        ArrangeKey = Key.F5.WithCtrl;

        KeyBindings.Add (QuitKey, KeyBindingScope.Application, Command.Quit);

        KeyBindings.Add (Key.CursorRight, KeyBindingScope.Application, Command.NextTabStop);
        KeyBindings.Add (Key.CursorDown, KeyBindingScope.Application, Command.NextTabStop);
        KeyBindings.Add (Key.CursorLeft, KeyBindingScope.Application, Command.PreviousTabStop);
        KeyBindings.Add (Key.CursorUp, KeyBindingScope.Application, Command.PreviousTabStop);
        KeyBindings.Add (NextTabKey, KeyBindingScope.Application, Command.NextTabStop);
        KeyBindings.Add (PrevTabKey, KeyBindingScope.Application, Command.PreviousTabStop);

        KeyBindings.Add (NextTabGroupKey, KeyBindingScope.Application, Command.NextTabGroup);
        KeyBindings.Add (PrevTabGroupKey, KeyBindingScope.Application, Command.PreviousTabGroup);

        KeyBindings.Add (ArrangeKey, KeyBindingScope.Application, Command.Edit);

        // TODO: Refresh Key should be configurable
        KeyBindings.Add (Key.F5, KeyBindingScope.Application, Command.Refresh);

        // TODO: Suspend Key should be configurable
        if (Environment.OSVersion.Platform == PlatformID.Unix)
        {
            KeyBindings.Add (Key.Z.WithCtrl, KeyBindingScope.Application, Command.Suspend);
        }
    }

    /// <summary>
    ///     <para>
    ///         Sets the function that will be invoked for a <see cref="Command"/>.
    ///     </para>
    ///     <para>
    ///         If AddCommand has already been called for <paramref name="command"/> <paramref name="f"/> will
    ///         replace the old one.
    ///     </para>
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This version of AddCommand is for commands that do not require a <see cref="CommandContext"/>.
    ///     </para>
    /// </remarks>
    /// <param name="command">The command.</param>
    /// <param name="f">The function.</param>
    protected void AddCommand (Command command, Func<bool?> f) { CommandImplementations! [command] = ctx => f (); }

    /// <summary>
    ///     Commands for Application.
    /// </summary>
    private static Dictionary<Command, View.CommandImplementation>? CommandImplementations { get; set; }

    // IMPORTANT: Ensure all property/fields are reset here. See Init_ResetState_Resets_Properties unit test.
    // Encapsulate all setting of initial state for Application; Having
    // this in a function like this ensures we don't make mistakes in
    // guaranteeing that the state of this singleton is deterministic when Init
    // starts running and after Shutdown returns.
    internal void ResetState (bool ignoreDisposed = false)
    {
        Application.Navigation = new ApplicationNavigation ();

        // Shutdown is the bookend for Init. As such it needs to clean up all resources
        // Init created. Apps that do any threading will need to code defensively for this.
        // e.g. see Issue #537
        foreach (Toplevel? t in TopLevels)
        {
            t!.Running = false;
        }

        TopLevels.Clear ();
#if DEBUG_IDISPOSABLE

        // Don't dispose the Top. It's up to caller dispose it
        if (!ignoreDisposed && Top is { })
        {
            Debug.Assert (Top.WasDisposed);

            // If End wasn't called _cachedRunStateToplevel may be null
            if (_cachedRunStateToplevel is { })
            {
                Debug.Assert (_cachedRunStateToplevel.WasDisposed);
                Debug.Assert (_cachedRunStateToplevel == Top);
            }
        }
#endif
        Top = null;
        _cachedRunStateToplevel = null;

        // MainLoop stuff
        MainLoop?.Dispose ();
        MainLoop = null;
        MainThreadId = -1;
        Iteration = null;
        EndAfterFirstIteration = false;

        // Driver stuff
        if (Driver is { })
        {
            Driver.SizeChanged -= Driver_SizeChanged;
            Driver.KeyDown -= Driver_KeyDown;
            Driver.KeyUp -= Driver_KeyUp;
            Driver.MouseEvent -= Driver_MouseEvent;
            Driver?.End ();
            Driver = null;
        }

        _screen = null;

        // Don't reset ForceDriver; it needs to be set before Init is called.
        //ForceDriver = string.Empty;
        //Force16Colors = false;
        _forceFakeConsole = false;

        // Run State stuff
        NotifyNewRunState = null;
        NotifyStopRunState = null;
        MouseGrabView = null;
        Initialized = false;

        // Mouse
        _lastMousePosition = null;
        _cachedViewsUnderMouse.Clear ();
        WantContinuousButtonPressedView = null;
        MouseEvent = null;
        GrabbedMouse = null;
        UnGrabbingMouse = null;
        GrabbedMouse = null;
        UnGrabbedMouse = null;

        // Keyboard
        KeyDown = null;
        KeyUp = null;
        SizeChanging = null;

        Navigation = null;

        ClearScreenNextIteration = false;

        AddApplicationKeyBindings ();

        // Reset synchronization context to allow the user to run async/await,
        // as the main loop has been ended, the synchronization context from
        // gui.cs does no longer process any callbacks. See #1084 for more details:
        // (https://github.com/gui-cs/Terminal.Gui/issues/1084).
        SynchronizationContext.SetSynchronizationContext (null);
    }
}
