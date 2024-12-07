using System.Collections.Concurrent;

namespace Terminal.Gui.ConsoleDrivers.V2;


public class ApplicationV2 : ApplicationImpl
{
    private IMainLoopCoordinator _coordinator;

    public ApplicationV2 ()
    {
        IsLegacy = false;
    }

    /// <inheritdoc />
    public override void Init (IConsoleDriver driver = null, string driverName = null)
    {
        Application.Navigation = new ();

        Application.AddApplicationKeyBindings ();

        CreateDriver ();

        Application.Initialized = true;

        Application.SubscribeDriverEvents ();
    }

    private void CreateDriver ()
    {

        PlatformID p = Environment.OSVersion.Platform;

        /*if ( p == PlatformID.Win32NT || p == PlatformID.Win32S || p == PlatformID.Win32Windows)
        {
            var inputBuffer = new ConcurrentQueue<InputRecord> ();
            var loop = new MainLoop<InputRecord> ();
            _coordinator = new MainLoopCoordinator<InputRecord> (
                                                                () => new WindowsInput (),
                                                                inputBuffer,
                                                                new WindowsInputProcessor (inputBuffer),
                                                                () => new WindowsOutput (),
                                                                loop);
        }
        else
        {*/
            var inputBuffer = new ConcurrentQueue<ConsoleKeyInfo> ();
            var loop = new MainLoop<ConsoleKeyInfo> ();
            _coordinator = new MainLoopCoordinator<ConsoleKeyInfo> (() => new NetInput (),
                                                                   inputBuffer,
                                                                   new NetInputProcessor (inputBuffer),
                                                                   () => new NetOutput (),
                                                                   loop);
        //}

        _coordinator.StartAsync ();

        if (!_coordinator.StartupSemaphore.WaitAsync (TimeSpan.FromSeconds (3)).Result)
        {
            throw new Exception ("Failed to boot MainLoopCoordinator in sensible timeframe");
        }

        if (Application.Driver == null)
        {
            throw new Exception ("Application.Driver was null even after booting MainLoopCoordinator");
        }
    }

    /// <inheritdoc />
    public override T Run<T> (Func<Exception, bool> errorHandler = null, IConsoleDriver driver = null)
    {
        var top = new T ();

        Run (top, errorHandler);

        return top;
    }

    /// <inheritdoc />
    public override void Run (Toplevel view, Func<Exception, bool> errorHandler = null)
    {
        ArgumentNullException.ThrowIfNull (view);

        if (!Application.Initialized)
        {
            throw new Exception ("App not Initialized");
        }

        Application.Top = view;

        Application.Begin (view);

        // TODO : how to know when we are done?
        while (Application.TopLevels.TryPeek (out var found) && found == view)
        {
            Task.Delay (100).Wait ();
        }
    }

    /// <inheritdoc />
    public override void Shutdown ()
    {
        _coordinator.Stop ();
        base.Shutdown ();
    }

    /// <inheritdoc />
    public override void RequestStop (Toplevel top)
    {
        Application.TopLevels.Pop ();

        if(Application.TopLevels.Count>0)
        {
            Application.Top = Application.TopLevels.Peek ();
        }
        else
        {
            Application.Top = null;
        }
    }

    /// <inheritdoc />
    public override void Invoke (Action action)
    {
        // TODO
    }

    /// <inheritdoc />
    public override void AddIdle (Func<bool> func)
    {
        // TODO properly

        func.Invoke ();
    }
}
