﻿namespace Terminal.Gui;

/// <summary>
///     Adornments are a special form of <see cref="View"/> that appear outside the <see cref="View.Viewport"/>:
///     <see cref="Margin"/>, <see cref="Border"/>, and <see cref="Padding"/>. They are defined using the
///     <see cref="Thickness"/> class, which specifies the thickness of the sides of a rectangle.
/// </summary>
/// <remarsk>
///     <para>
///         Each of <see cref="Margin"/>, <see cref="Border"/>, and <see cref="Padding"/> has slightly different
///         behavior relative to <see cref="ColorScheme"/>, <see cref="View.SetFocus"/>, keyboard input, and
///         mouse input. Each can be customized by manipulating their Subviews.
///     </para>
/// </remarsk>
public class Adornment : View
{
    /// <inheritdoc/>
    public Adornment ()
    {
        /* Do nothing; A parameter-less constructor is required to support all views unit tests. */
    }

    /// <summary>Constructs a new adornment for the view specified by <paramref name="parent"/>.</summary>
    /// <param name="parent"></param>
    public Adornment (View parent)
    {
        Application.GrabbingMouse += Application_GrabbingMouse;
        Application.UnGrabbingMouse += Application_UnGrabbingMouse;
        CanFocus = true;
        Parent = parent;
    }

    /// <summary>The Parent of this Adornment (the View this Adornment surrounds).</summary>
    /// <remarks>
    ///     Adornments are distinguished from typical View classes in that they are not sub-views, but have a parent/child
    ///     relationship with their containing View.
    /// </remarks>
    public View Parent { get; set; }

    #region Thickness

    private Thickness _thickness = Thickness.Empty;

    /// <summary>Defines the rectangle that the <see cref="Adornment"/> will use to draw its content.</summary>
    public Thickness Thickness
    {
        get => _thickness;
        set
        {
            Thickness prev = _thickness;
            _thickness = value;

            if (prev != _thickness)
            {
                if (Parent?.IsInitialized == false)
                {
                    // When initialized Parent.LayoutSubViews will cause a LayoutAdornments
                    Parent?.LayoutAdornments ();
                }
                else
                {
                    Parent?.SetNeedsLayout ();
                    Parent?.LayoutSubviews ();
                }

                OnThicknessChanged (prev);
            }
        }
    }

    /// <summary>Fired whenever the <see cref="Thickness"/> property changes.</summary>
    public event EventHandler<ThicknessEventArgs> ThicknessChanged;

    /// <summary>Called whenever the <see cref="Thickness"/> property changes.</summary>
    public void OnThicknessChanged (Thickness previousThickness)
    {
        ThicknessChanged?.Invoke (
                                  this,
                                  new () { Thickness = Thickness, PreviousThickness = previousThickness }
                                 );
    }

    #endregion Thickness

    #region View Overrides

    /// <summary>
    ///     Adornments cannot be used as sub-views (see <see cref="Parent"/>); setting this property will throw
    ///     <see cref="InvalidOperationException"/>.
    /// </summary>
    public override View SuperView
    {
        get => null;
        set => throw new InvalidOperationException (@"Adornments can not be Subviews or have SuperViews. Use Parent instead.");
    }

    //internal override Adornment CreateAdornment (Type adornmentType)
    //{
    //    /* Do nothing - Adornments do not have Adornments */
    //    return null;
    //}

    internal override void LayoutAdornments ()
    {
        /* Do nothing - Adornments do not have Adornments */
    }

    /// <summary>
    ///     Gets the rectangle that describes the area of the Adornment. The Location is always (0,0).
    ///     The size is the size of the <see cref="View.Frame"/>.
    /// </summary>
    /// <remarks>
    ///     The Viewport of an Adornment cannot be modified. Attempting to set this property will throw an
    ///     <see cref="InvalidOperationException"/>.
    /// </remarks>
    public override Rectangle Viewport
    {
        get => Frame with { Location = Point.Empty };
        set => throw new InvalidOperationException (@"The Viewport of an Adornment cannot be modified.");
    }

    /// <inheritdoc/>
    public override Rectangle FrameToScreen ()
    {
        if (Parent is null)
        {
            return Frame;
        }

        // Adornments are *Children* of a View, not SubViews. Thus View.FrameToScreen will not work.
        // To get the screen-relative coordinates of an Adornment, we need get the parent's Frame
        // in screen coords, and add our Frame location to it.
        Rectangle parent = Parent.FrameToScreen ();

        return new (new (parent.X + Frame.X, parent.Y + Frame.Y), Frame.Size);
    }

    /// <inheritdoc/>
    public override Point ScreenToFrame (int x, int y) { return Parent.ScreenToFrame (x - Frame.X, y - Frame.Y); }

    /// <summary>Does nothing for Adornment</summary>
    /// <returns></returns>
    public override bool OnDrawAdornments () { return false; }

    /// <summary>Redraws the Adornments that comprise the <see cref="Adornment"/>.</summary>
    public override void OnDrawContent (Rectangle viewport)
    {
        if (Thickness == Thickness.Empty)
        {
            return;
        }

        Rectangle screen = ViewportToScreen (viewport);
        Attribute normalAttr = GetNormalColor ();
        Driver.SetAttribute (normalAttr);

        // This just draws/clears the thickness, not the insides.
        Thickness.Draw (screen, ToString ());

        if (!string.IsNullOrEmpty (TextFormatter.Text))
        {
            if (TextFormatter is { })
            {
                TextFormatter.Size = Frame.Size;
                TextFormatter.NeedsFormat = true;
            }
        }

        TextFormatter?.Draw (screen, normalAttr, normalAttr, Rectangle.Empty);

        if (Subviews.Count > 0)
        {
            base.OnDrawContent (viewport);
        }

        ClearLayoutNeeded ();
        ClearNeedsDisplay ();
    }

    /// <summary>Does nothing for Adornment</summary>
    /// <returns></returns>
    public override bool OnRenderLineCanvas () { return false; }

    /// <summary>
    ///     Adornments only render to their <see cref="Parent"/>'s or Parent's SuperView's LineCanvas, so setting this
    ///     property throws an <see cref="InvalidOperationException"/>.
    /// </summary>
    public override bool SuperViewRendersLineCanvas
    {
        get => false; 
        set => throw new InvalidOperationException (@"Adornment can only render to their Parent or Parent's Superview.");
    }

    #endregion View Overrides

    #region Mouse Support


    /// <summary>
    /// Indicates whether the specified Parent's SuperView-relative coordinates are within the Adornment's Thickness.
    /// </summary>
    /// <remarks>
    ///     The <paramref name="x"/> and <paramref name="x"/> are relative to the PARENT's SuperView.
    /// </remarks>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns><see langword="true"/> if the specified Parent's SuperView-relative coordinates are within the Adornment's Thickness. </returns>
    public override bool Contains (int x, int y)
    {
        if (Parent is null)
        {
            return false;
        }
        Rectangle frame = Frame;
        frame.Offset (Parent.Frame.Location);

        return Thickness.Contains (frame, x, y);
    }

    private Point? _dragPosition;
    private Point _startGrabPoint;

    /// <inheritdoc/>
    protected internal override bool OnMouseEnter (MouseEvent mouseEvent)
    {
        // Invert Normal
        if (Diagnostics.HasFlag (ViewDiagnosticFlags.MouseEnter) && ColorScheme != null)
        {
            var cs = new ColorScheme (ColorScheme)
            {
                Normal = new (ColorScheme.Normal.Background, ColorScheme.Normal.Foreground)
            };
            ColorScheme = cs;
        }

        return base.OnMouseEnter (mouseEvent);
    }

    /// <summary>Called when a mouse event occurs within the Adornment.</summary>
    /// <remarks>
    ///     <para>
    ///         The coordinates are relative to <see cref="View.Viewport"/>.
    ///     </para>
    ///     <para>
    ///         A mouse click on the Adornment will cause the Parent to focus.
    ///     </para>
    ///     <para>
    ///         A mouse drag on the Adornment will cause the Parent to move.
    ///     </para>
    /// </remarks>
    /// <param name="mouseEvent"></param>
    /// <returns><see langword="true"/>, if the event was handled, <see langword="false"/> otherwise.</returns>
    protected internal override bool OnMouseEvent (MouseEvent mouseEvent)
    {
        var args = new MouseEventEventArgs (mouseEvent);

        if (mouseEvent.Flags.HasFlag (MouseFlags.Button1Clicked))
        {
            if (Parent.CanFocus && !Parent.HasFocus)
            {
                Parent.SetFocus ();
                Parent.SetNeedsDisplay ();
            }

            return OnMouseClick (args);
        }

        if (!Parent.CanFocus || !Parent.Arrangement.HasFlag (ViewArrangement.Movable))
        {
            return true;
        }

        // BUGBUG: See https://github.com/gui-cs/Terminal.Gui/issues/3312
        if (!_dragPosition.HasValue && mouseEvent.Flags.HasFlag (MouseFlags.Button1Pressed))
        {
            Parent.SetFocus ();
            Application.BringOverlappedTopToFront ();

            // Only start grabbing if the user clicks in the Thickness area
            // Adornment.Contains takes Parent SuperView=relative coords.
            if (Contains (mouseEvent.X + Parent.Frame.X + Frame.X, mouseEvent.Y+ Parent.Frame.Y + Frame.Y))
            {
                // Set the start grab point to the Frame coords
                _startGrabPoint = new (mouseEvent.X + Frame.X, mouseEvent.Y + Frame.Y);
                _dragPosition = new (mouseEvent.X, mouseEvent.Y);
                Application.GrabMouse (this);
            }

            return true;
        }

        if (mouseEvent.Flags is (MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition))
        {
            if (Application.MouseGrabView == this && _dragPosition.HasValue)
            {
                if (Parent.SuperView is null)
                {
                    // Redraw the entire app window.
                    Application.Top.SetNeedsDisplay ();
                }
                else
                {
                    Parent.SuperView.SetNeedsDisplay ();
                }

                _dragPosition = new Point (mouseEvent.X, mouseEvent.Y);

                Point parentLoc = Parent.SuperView?.ScreenToViewport (mouseEvent.ScreenPosition.X, mouseEvent.ScreenPosition.Y) ?? mouseEvent.ScreenPosition;

                GetLocationEnsuringFullVisibility (
                                     Parent,
                                     parentLoc.X - _startGrabPoint.X,
                                     parentLoc.Y - _startGrabPoint.Y,
                                     out int nx,
                                     out int ny,
                                     out _
                                    );

                Parent.X = nx;
                Parent.Y = ny;

                return true;
            }
        }

        if (mouseEvent.Flags.HasFlag (MouseFlags.Button1Released) && _dragPosition.HasValue)
        {
            _dragPosition = null;
            Application.UngrabMouse ();
        }

        return false;
    }

    /// <inheritdoc/>
    protected internal override bool OnMouseLeave (MouseEvent mouseEvent)
    {
        // Invert Normal
        if (Diagnostics.HasFlag (ViewDiagnosticFlags.MouseEnter) && ColorScheme != null)
        {
            var cs = new ColorScheme (ColorScheme)
            {
                Normal = new (ColorScheme.Normal.Background, ColorScheme.Normal.Foreground)
            };
            ColorScheme = cs;
        }

        return base.OnMouseLeave (mouseEvent);
    }

    /// <inheritdoc/>
    protected override void Dispose (bool disposing)
    {
        Application.GrabbingMouse -= Application_GrabbingMouse;
        Application.UnGrabbingMouse -= Application_UnGrabbingMouse;

        _dragPosition = null;
        base.Dispose (disposing);
    }

    private void Application_GrabbingMouse (object sender, GrabMouseEventArgs e)
    {
        if (Application.MouseGrabView == this && _dragPosition.HasValue)
        {
            e.Cancel = true;
        }
    }

    private void Application_UnGrabbingMouse (object sender, GrabMouseEventArgs e)
    {
        if (Application.MouseGrabView == this && _dragPosition.HasValue)
        {
            e.Cancel = true;
        }
    }
    #endregion Mouse Support
}
