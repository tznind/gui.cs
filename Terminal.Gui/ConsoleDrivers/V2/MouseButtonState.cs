#nullable enable
namespace Terminal.Gui;

/// <summary>
/// Not to be confused with <see cref="NetEvents.MouseButtonState"/>
/// </summary>
internal class MouseButtonStateEx
{
    public required int Button { get; set; }
    /// <summary>
    ///     When the button entered its current state.
    /// </summary>
    public DateTime At { get; set; }

    /// <summary>
    /// <see langword="true"/> if the button is currently down
    /// </summary>
    public bool Pressed { get; set; }

    /// <summary>
    /// The screen location when the mouse button entered its current state
    /// (became pressed or was released)
    /// </summary>
    public Point Position { get; set; }

    /// <summary>
    /// The <see cref="View"/> (if any) that was at the <see cref="Position"/>
    /// when the button entered its current state.
    /// </summary>
    public View? View { get; set; }

    /// <summary>
    /// Viewport relative position within <see cref="View"/> (if there is one)
    /// </summary>
    public Point ViewportPosition { get; set; }

    /// <summary>
    /// True if shift was provided by the console at the time the mouse
    ///  button entered its current state.
    /// </summary>
    public bool Shift { get; set; }

    /// <summary>
    /// True if control was provided by the console at the time the mouse
    /// button entered its current state.
    /// </summary>
    public bool Ctrl { get; set; }

    /// <summary>
    /// True if alt was held down at the time the mouse
    /// button entered its current state.
    /// </summary>
    public bool Alt { get; set; }
}
