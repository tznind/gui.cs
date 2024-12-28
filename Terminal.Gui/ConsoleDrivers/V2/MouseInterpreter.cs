#nullable enable

namespace Terminal.Gui;

internal class MouseInterpreter
{
    private readonly IViewFinder _viewFinder;

    /// <summary>
    /// Function for returning the current time. Use in unit tests to
    /// ensure repeatable tests.
    /// </summary>
    public Func<DateTime> Now { get; set; }

    /// <summary>
    /// How long to wait for a second, third, fourth click after the first before giving up and
    /// releasing event as a 'click'
    /// </summary>
    public TimeSpan RepeatedClickThreshold { get; set; }

    /// <summary>
    /// How far between a mouse down and mouse up before it is considered a 'drag' rather
    /// than a 'click'. Console row counts for 2 units while column counts for only 1. Distance is
    /// measured in Euclidean distance.
    /// </summary>
    public double DragThreshold { get; set; }

    public MouseState CurrentState { get; private set; }

    private MouseButtonSequence? [] _ongoingSequences = new MouseButtonSequence? [4];
    private readonly bool[] _lastPressed = new bool [4];

    public Action<MouseButtonSequence> Click { get; set; }

    public MouseInterpreter (
        Func<DateTime>? now = null,
        IViewFinder viewFinder = null,
        TimeSpan? doubleClickThreshold = null,
        int dragThreshold = 5
    )
    {
        _viewFinder = viewFinder ?? new StaticViewFinder ();
        Now = now ?? (() => DateTime.Now);
        RepeatedClickThreshold = doubleClickThreshold ?? TimeSpan.FromMilliseconds (500);
        DragThreshold = dragThreshold;
    }

    public IEnumerable<MouseEventArgs> Process (MouseEventArgs e)
    {
        // For each mouse button
        for (int i = 0; i < 4; i++)
        {
            bool isPressed = IsPressed (i, e.Flags);
            var sequence = _ongoingSequences [i];

            // If we have no ongoing narratives
            if (sequence == null)
            {
                // Changing from not pressed to pressed
                if (isPressed && isPressed != _lastPressed [i])
                {
                    // Begin sequence that leads to click/double click/triple click etc
                    _ongoingSequences [i] = BeginPressedNarrative (i, e);
                }
            }
            else
            {
                var resolve = sequence.Process (e.Position, isPressed);

                if (sequence.IsResolved)
                {
                    _ongoingSequences [i] = null;
                }

                if (resolve != null)
                {
                    yield return resolve;
                }
            }

            _lastPressed [i] = isPressed;
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

        return new MouseButtonSequence(this,buttonIdx,_viewFinder)
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