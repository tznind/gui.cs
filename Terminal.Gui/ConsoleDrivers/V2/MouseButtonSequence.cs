#nullable enable
using Terminal.Gui.ConsoleDrivers.V2;

namespace Terminal.Gui;

/// <summary>
/// Describes a completed narrative e.g. 'user triple clicked'.
/// </summary>
/// <remarks>Internally we can have a double click narrative that becomes
/// a triple click narrative. But we will not release both i.e. we don't say
/// user clicked then user double-clicked then user triple clicked</remarks>
internal class MouseButtonSequence
{
    private readonly IViewFinder _viewFinder;
    public int NumberOfClicks { get; set; }

    /// <summary>
    /// Mouse states during which click was generated.
    /// N = 2x<see cref="NumberOfClicks"/>
    /// </summary>
    public List<MouseButtonStateEx> MouseStates { get; set; } = new ();

    public int ButtonIdx { get; }

    /// <summary>
    /// Function for returning the current time. Use in unit tests to
    /// ensure repeatable tests.
    /// </summary>
    public Func<DateTime> Now { get; set; }

    public MouseButtonSequence (int buttonIdx, Func<DateTime> now, IViewFinder viewFinder)
    {
        ButtonIdx = buttonIdx;
        Now = now;
        _viewFinder = viewFinder;
    }

    public MouseEventArgs? Process (Point position, bool pressed)
    {
        var last = MouseStates.Last ();

        // Still pressed
        if (last.Pressed && pressed)
        {
            // No change
            return null;
        }

        var view = _viewFinder.GetViewAt (position, out var viewport);

        NumberOfClicks++;
        MouseStates.Add (new MouseButtonStateEx
        {
            Button = ButtonIdx,
            At = Now(),
            Pressed = false,
            Position = position,

            View = view,
            ViewportPosition = viewport,

            /* TODO: Need these probably*/
            Shift = false,
            Ctrl = false,
            Alt = false,

        });


        if (IsResolveable ())
        {
            return Resolve ();
        }

        return null;
    }

    public bool IsResolveable ()
    {
        return NumberOfClicks > 0;
    }
    /// <summary>
    /// Resolves the narrative completely with immediate effect.
    /// This may return null if e.g. so far all that has accumulated is a mouse down.
    /// </summary>
    /// <returns></returns>
    public MouseEventArgs? Resolve ()
    {
        if (!IsResolveable ())
        {
            return null;
        }

        if (NumberOfClicks == 1)
        {
            var last = MouseStates.Last ();

            // its a click
            return new MouseEventArgs
            {
                Handled = false,
                Flags = ToClicks(this.ButtonIdx,NumberOfClicks),
                ScreenPosition = last.Position,
                Position = last.ViewportPosition,
                View = last.View,
            };
        }

        return null;
    }

    private MouseFlags ToClicks (int buttonIdx, int numberOfClicks)
    {
        if (numberOfClicks == 0)
        {
            throw new ArgumentOutOfRangeException (nameof (numberOfClicks), "Zero clicks are not valid.");
        }

        return buttonIdx switch
               {
                   0 => numberOfClicks switch
                        {
                            1 => MouseFlags.Button1Clicked,
                            2 => MouseFlags.Button1DoubleClicked,
                            _ => MouseFlags.Button1TripleClicked
                        },
                   1 => numberOfClicks switch
                        {
                            1 => MouseFlags.Button2Clicked,
                            2 => MouseFlags.Button2DoubleClicked,
                            _ => MouseFlags.Button2TripleClicked
                        },
                   2 => numberOfClicks switch
                        {
                            1 => MouseFlags.Button3Clicked,
                            2 => MouseFlags.Button3DoubleClicked,
                            _ => MouseFlags.Button3TripleClicked
                        },
                   3 => numberOfClicks switch
                        {
                            1 => MouseFlags.Button4Clicked,
                            2 => MouseFlags.Button4DoubleClicked,
                            _ => MouseFlags.Button4TripleClicked
                        },
                   _ => throw new ArgumentOutOfRangeException (nameof (buttonIdx), "Unsupported button index")
               };
    }
}
