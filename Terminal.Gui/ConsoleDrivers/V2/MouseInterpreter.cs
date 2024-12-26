#nullable enable

namespace Terminal.Gui;

internal class MouseInterpreter
{
    private readonly IViewFinder _viewFinder;

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

    private MouseButtonSequence? [] _ongoingSequences = new MouseButtonSequence? [4];

    public Action<MouseButtonSequence> Click { get; set; }

    public MouseInterpreter (
        Func<DateTime>? now = null,
        IViewFinder viewFinder = null,
        TimeSpan? doubleClickThreshold = null,
        TimeSpan? tripleClickThreshold = null,
        int dragThreshold = 5
    )
    {
        _viewFinder = viewFinder ?? new StaticViewFinder ();
        Now = now ?? (() => DateTime.Now);
        DoubleClickThreshold = doubleClickThreshold ?? TimeSpan.FromMilliseconds (500);
        TripleClickThreshold = tripleClickThreshold ?? TimeSpan.FromMilliseconds (1000);
        DragThreshold = dragThreshold;
    }

    public IEnumerable<MouseEventArgs> Process (MouseEventArgs e)
    {
        // For each mouse button
        for (int i = 0; i < 4; i++)
        {
            bool isPressed = IsPressed (i, e.Flags);

            // Update narratives
            if (isPressed)
            {
                if (_ongoingSequences [i] == null)
                {
                    _ongoingSequences [i] = BeginPressedNarrative (i, e);
                }
                else
                {
                    var resolve = _ongoingSequences [i]?.Process (e.Position, true);

                    if (resolve != null)
                    {
                        _ongoingSequences [i] = null;
                        yield return resolve;
                    }
                }
            }
            else
            {
                var resolve = _ongoingSequences [i]?.Process (e.Position, false);

                if (resolve != null)
                {

                    _ongoingSequences [i] = null;
                    yield return resolve;
                }
            }
        }
    }

    public IEnumerable<MouseEventArgs> Release ()
    {
        for (var i = 0; i < _ongoingSequences.Length; i++)
        {
            MouseButtonSequence? narrative = _ongoingSequences [i];

            if (narrative != null)
            {
                if (narrative.IsResolveable ())
                {
                    var args = narrative.Resolve ();

                    if (args != null)
                    {
                        yield return args;
                    }
                    _ongoingSequences [i] = null;
                }
            }
        }
    }

    private bool IsPressed (int btn, MouseFlags eFlags)
    {
        return btn switch
               {
                   0=>eFlags.HasFlag (MouseFlags.Button1Pressed),
                   1 => eFlags.HasFlag (MouseFlags.Button2Pressed),
                   2 => eFlags.HasFlag (MouseFlags.Button3Pressed),
                   3 => eFlags.HasFlag (MouseFlags.Button4Pressed),
                   _ => throw new ArgumentOutOfRangeException(nameof(btn))
               };
    }

    private MouseButtonSequence BeginPressedNarrative (int buttonIdx, MouseEventArgs e)
    {
        var view = _viewFinder.GetViewAt (e.Position, out var viewport);

        return new MouseButtonSequence(buttonIdx, Now,_viewFinder)
        {
            NumberOfClicks = 0,
            MouseStates =
            [
                new MouseButtonStateEx()
                {
                    Button = buttonIdx,
                    At = Now(),
                    Pressed = true,
                    Position = e.ScreenPosition,
                    View = view,
                    ViewportPosition = viewport,

                    /* TODO: Do these too*/
                    Shift = false,
                    Ctrl = false,
                    Alt = false
                }
            ]
        };
    }

    public Point ViewportPosition { get; set; }

    /* TODO: Probably need this at some point
    public static double DistanceTo (Point p1, Point p2)
    {
        int deltaX = p2.X - p1.X;
        int deltaY = p2.Y - p1.Y;
        return Math.Sqrt (deltaX * deltaX + deltaY * deltaY);
    }*/
}
