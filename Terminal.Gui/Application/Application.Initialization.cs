#nullable enable
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Terminal.Gui;

public static partial class Application // Initialization (Init/Shutdown)
{
    /// <summary>Initializes a new instance of <see cref="Terminal.Gui"/> Application.</summary>
    /// <para>Call this method once per instance (or after <see cref="Shutdown"/> has been called).</para>
    /// <para>
    ///     This function loads the right <see cref="IConsoleDriver"/> for the platform, Creates a <see cref="Toplevel"/>. and
    ///     assigns it to <see cref="Top"/>
    /// </para>
    /// <para>
    ///     <see cref="Shutdown"/> must be called when the application is closing (typically after
    ///     <see cref="Run{T}"/> has returned) to ensure resources are cleaned up and
    ///     terminal settings
    ///     restored.
    /// </para>
    /// <para>
    ///     The <see cref="Run{T}"/> function combines
    ///     <see cref="Init(Terminal.Gui.IConsoleDriver,string)"/> and <see cref="Run(Toplevel, Func{Exception, bool})"/>
    ///     into a single
    ///     call. An application cam use <see cref="Run{T}"/> without explicitly calling
    ///     <see cref="Init(Terminal.Gui.IConsoleDriver,string)"/>.
    /// </para>
    /// <param name="driver">
    ///     The <see cref="IConsoleDriver"/> to use. If neither <paramref name="driver"/> or
    ///     <paramref name="driverName"/> are specified the default driver for the platform will be used.
    /// </param>
    /// <param name="driverName">
    ///     The short name (e.g. "net", "windows", "ansi", "fake", or "curses") of the
    ///     <see cref="IConsoleDriver"/> to use. If neither <paramref name="driver"/> or <paramref name="driverName"/> are
    ///     specified the default driver for the platform will be used.
    /// </param>
    [RequiresUnreferencedCode ("AOT")]
    [RequiresDynamicCode ("AOT")]
    public static void Init (IConsoleDriver? driver = null, string? driverName = null) { InternalInit (driver, driverName); }

    internal static int MainThreadId { get; set; } = -1;

    

    private static void Driver_SizeChanged (object? sender, SizeChangedEventArgs e) { OnSizeChanging (e); }
    private static void Driver_KeyDown (object? sender, Key e) { RaiseKeyDownEvent (e); }
    private static void Driver_KeyUp (object? sender, Key e) { RaiseKeyUpEvent (e); }
    private static void Driver_MouseEvent (object? sender, MouseEventArgs e) { RaiseMouseEvent (e); }

    /// <summary>Gets of list of <see cref="IConsoleDriver"/> types that are available.</summary>
    /// <returns></returns>
    [RequiresUnreferencedCode ("AOT")]
    public static List<Type?> GetDriverTypes ()
    {
        // use reflection to get the list of drivers
        List<Type?> driverTypes = new ();

        foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies ())
        {
            foreach (Type? type in asm.GetTypes ())
            {
                if (typeof (IConsoleDriver).IsAssignableFrom (type) && !type.IsAbstract && type.IsClass)
                {
                    driverTypes.Add (type);
                }
            }
        }

        return driverTypes;
    }

    /// <summary>Shutdown an application initialized with <see cref="Init"/>.</summary>
    /// <remarks>
    ///     Shutdown must be called for every call to <see cref="Init"/> or
    ///     <see cref="Application.Run(Toplevel, Func{Exception, bool})"/> to ensure all resources are cleaned
    ///     up (Disposed)
    ///     and terminal settings are restored.
    /// </remarks>
    public static void Shutdown ()
    {
        // TODO: Throw an exception if Init hasn't been called.

        bool wasInitialized = Initialized;
        ResetState ();
        PrintJsonErrors ();

        if (wasInitialized)
        {
            bool init = Initialized;
            InitializedChanged?.Invoke (null, new (in init));
        }
    }

    /// <summary>
    ///     Gets whether the application has been initialized with <see cref="Init"/> and not yet shutdown with <see cref="Shutdown"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    ///     The <see cref="InitializedChanged"/> event is raised after the <see cref="Init"/> and <see cref="Shutdown"/> methods have been called.
    /// </para>
    /// </remarks>
    public static bool Initialized { get; internal set; }

    /// <summary>
    ///     This event is raised after the <see cref="Init"/> and <see cref="Shutdown"/> methods have been called.
    /// </summary>
    /// <remarks>
    ///     Intended to support unit tests that need to know when the application has been initialized.
    /// </remarks>
    public static event EventHandler<EventArgs<bool>>? InitializedChanged;
}
