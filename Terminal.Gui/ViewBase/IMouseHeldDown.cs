#nullable enable
using System.ComponentModel;

namespace Terminal.Gui.ViewBase;

/// <summary>
///     <para>
///         Handler for raising periodic events while the mouse is held down.
///         Typically, mouse pointer only needs to be pressed down in a view
///         to begin this event after which it can be moved elsewhere.
///     </para>
///     <para>
///         Common use cases for this includes holding a button down to increase
///         a counter (e.g. in <see cref="NumericUpDown"/>).
///     </para>
/// </summary>
public interface IMouseHeldDown : IDisposable
{
    // TODO: Guess this should follow the established events type - need to double check what that is.
    public event EventHandler<CancelEventArgs> MouseIsHeldDownTick;

    void Start ();
    void Stop ();
}
