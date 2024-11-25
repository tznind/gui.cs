#nullable enable

namespace Terminal.Gui;
/*
public class MouseStateManager
{
    /// <summary>
    /// Function for returning the current time. Use in unit tests to
    /// ensure repeatable tests.
    /// </summary>
    private Func<DateTime> Now { get; set; }

    /// <summary>
    /// How long to wait for a second click after the first before giving up and
    /// releasing event as a 'click'
    /// </summary>
    public TimeSpan DoubleClickThreshold { get; set; }

    /// <summary>
    /// How long to wait for a third click after the second before giving up and
    /// releasing event as a 'double click'
    /// </summary>
    public TimeSpan TripleClickThreshold { get; set; }

    /// <summary>
    /// How far between a mouse down and mouse up before it is considered a 'drag' rather
    /// than a 'click'. Console row counts for 2 units while column counts for only 1. Distance is
    /// measured in Euclidean distance.
    /// </summary>
    public double DragThreshold { get; set; }

    public MouseState CurrentState { get; private set; }

    private ButtonNarrative? [] _ongoingNarratives = new ButtonNarrative? [4];

    public MouseStateManager (
        Func<DateTime>? now = null,
        TimeSpan? doubleClickThreshold = null,
        TimeSpan? tripleClickThreshold = null,
        int dragThreshold = 5
    )
    {
        Now = now ?? (() => DateTime.Now);
        DoubleClickThreshold = doubleClickThreshold ?? TimeSpan.FromMilliseconds (500);
        TripleClickThreshold = tripleClickThreshold ?? TimeSpan.FromMilliseconds (1000);
        DragThreshold = dragThreshold;
    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="e"></param>
    /// <returns></returns>
    public IEnumerable<ButtonNarrative> UpdateState (MouseEventArgs e)
    {
        // TODO: manage transitions

        for (int i = 0; i < 4; i++)
        {
           // Update narratives

           // Release stale or naturally complete ones based on thresholds
        }
    }

    /// <summary>
    /// If user double clicks and we are waiting for a triple click
    /// we should give up after a short time and just assume no more
    /// clicks are coming. Call this method if the state for a given button
    /// has not changed in a while.
    /// </summary>
    /// <returns></returns>
    public ButtonNarrative? ReleaseState (int button)
    {
        
    }

    private void PromoteSingleToDoubleClick ()
    {

    }
    private void PromoteDoubleToTripleClick ()
    {

    }

    public static double DistanceTo (Point p1, Point p2)
    {
        int deltaX = p2.X - p1.X;
        int deltaY = p2.Y - p1.Y;
        return Math.Sqrt (deltaX * deltaX + deltaY * deltaY);
    }
}

/// <summary>
/// Describes a completed narrative e.g. 'user triple clicked'.
/// </summary>
/// <remarks>Internally we can have a double click narrative that becomes
/// a triple click narrative. But we will not release both i.e. we don't say
/// user clicked then user double-clicked then user triple clicked</remarks>
public class ButtonNarrative
{
    public int Button { get; set; }
    public int NumberOfClicks { get; set; }

    /// <summary>
    /// Mouse states during which click was generated.
    /// N = 2x<see cref="NumberOfClicks"/>
    /// </summary>
    public List<ButtonState> MouseStates { get; set; }

    /// <summary>
    /// <see langword="true"/> if distance between first mouse down and all
    /// subsequent events is greater than a given threshold.
    /// </summary>
    public bool IsDrag { get; set; }
}

public class MouseState
{
    public ButtonState[] ButtonStates = new ButtonState? [4];

    public Point Position;
}

public class ButtonState
{
    /// <summary>
    ///     When the button entered its current state.
    /// </summary>
    public DateTime At { get; set; }

    /// <summary>
    /// <see langword="true"/> if the button is currently down
    /// </summary>
    private bool Depressed { get; set; }

    /// <summary>
    /// The screen location when the mouse button entered its current state
    /// (became depressed or was released)
    /// </summary>
    public Point Position { get; set; }

    /// <summary>
    /// The <see cref="View"/> (if any) that was at the <see cref="Position"/>
    /// when the button entered its current state.
    /// </summary>
    public View? View { get; set; }

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
}*/