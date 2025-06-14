#nullable enable
using System.ComponentModel;

namespace Terminal.Gui.ViewBase;

internal class MouseHeldDown : IMouseHeldDown
{
    private readonly View _host;
    private bool _down;
    private object? _timeout;

    public MouseHeldDown (View host, ITimedEvents timedEvents, IMouseGrabHandler mouseGrabber) { _host = host; }

    public event EventHandler<CancelEventArgs>? MouseIsHeldDownTick;

    public bool RaiseMouseIsHeldDownTick ()
    {
        CancelEventArgs args = new ();

        args.Cancel = OnMouseIsHeldDownTick (args) || args.Cancel;

        if (!args.Cancel && MouseIsHeldDownTick is { })
        {
            MouseIsHeldDownTick?.Invoke (this, args);
        }

        // User event cancelled the mouse held down status so
        // stop the currently running operation.
        if (args.Cancel)
        {
            Stop ();
        }

        return args.Cancel;
    }

    protected virtual bool OnMouseIsHeldDownTick (CancelEventArgs eventArgs) { return false; }

    public void Start ()
    {
        if (_down)
        {
            return;
        }

        _down = true;
        Application.MouseGrabHandler.GrabMouse (_host);

        // Then periodic ticks
        _timeout = Application.AddTimeout (TimeSpan.FromMilliseconds (500), TickWhileMouseIsHeldDown);
    }

    private bool TickWhileMouseIsHeldDown ()
    {
        Logging.Debug ("Raising TickWhileMouseIsHeldDown...");
        if (_down)
        {
            RaiseMouseIsHeldDownTick ();
        }
        else
        {
            Stop ();
        }

        return _down;
    }

    public void Stop ()
    {
        if (Application.MouseGrabHandler.MouseGrabView == _host)
        {
            Application.MouseGrabHandler.UngrabMouse ();
        }

        if (_timeout != null)
        {
            Application.RemoveTimeout (_timeout);
        }

        _down = false;
    }

    public void Dispose ()
    {
        if (Application.MouseGrabHandler.MouseGrabView == _host)
        {
            Stop ();
        }
    }
}
