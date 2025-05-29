﻿#nullable enable

namespace Terminal.Gui;

/// <summary>The Margin for a <see cref="View"/>. Accessed via <see cref="View.Margin"/></summary>
/// <remarks>
///     <para>
///         The Margin is transparent by default. This can be overriden by explicitly setting <see cref="ColorScheme"/>.
///     </para>
///     <para>
///         Margins are drawn after all other Views in the application View hierarchy are drawn.
///     </para>
///     <para>See the <see cref="Adornment"/> class.</para>
/// </remarks>
public class Margin : Adornment
{
    private const int SHADOW_WIDTH = 1;
    private const int SHADOW_HEIGHT = 1;
    private const int PRESS_MOVE_HORIZONTAL = 1;
    private const int PRESS_MOVE_VERTICAL = 0;

    /// <inheritdoc/>
    public Margin ()
    { /* Do nothing; A parameter-less constructor is required to support all views unit tests. */
    }

    /// <inheritdoc/>
    public Margin (View parent) : base (parent)
    {
        /* Do nothing; View.CreateAdornment requires a constructor that takes a parent */

        // BUGBUG: We should not set HighlightStyle.Pressed here, but wherever it is actually needed
        // HighlightStyle |= HighlightStyle.Pressed;
        Highlight += Margin_Highlight;
        SubViewLayout += Margin_LayoutStarted;

        // Margin should not be focusable
        CanFocus = false;
    }

    // When the Parent is drawn, we cache the clip region so we can draw the Margin after all other Views
    // QUESTION: Why can't this just be the NeedsDisplay region?
    private Region? _cachedClip;

    internal Region? GetCachedClip () { return _cachedClip; }

    internal void ClearCachedClip () { _cachedClip = null; }

    internal void CacheClip ()
    {
        if (Thickness != Thickness.Empty && ShadowStyle != ShadowStyle.None)
        {
            // PERFORMANCE: How expensive are these clones?
            _cachedClip = GetClip ()?.Clone ();
        }
    }

    // PERFORMANCE: Margins are ALWAYS drawn. This may be an issue for apps that have a large number of views with shadows.
    /// <summary>
    ///     INTERNAL API - Draws the margins for the specified views. This is called by the <see cref="Application"/> on each
    ///     iteration of the main loop after all Views have been drawn.
    /// </summary>
    /// <param name="margins"></param>
    /// <returns><see langword="true"/></returns>
    internal static bool DrawMargins (IEnumerable<View> margins)
    {
        Stack<View> stack = new (margins);

        while (stack.Count > 0)
        {
            var view = stack.Pop ();

            if (view.Margin?.GetCachedClip() != null)
            {
                view.Margin.NeedsDraw = true;
                Region? saved = GetClip ();
                View.SetClip (view.Margin.GetCachedClip ());
                view.Margin.Draw ();
                View.SetClip (saved);
                view.Margin.ClearCachedClip ();
            }

            view.NeedsDraw = false;

            foreach (var subview in view.SubViews)
            {
                stack.Push (subview);
            }
        }

        return true;
    }

    /// <inheritdoc/>
    public override void BeginInit ()
    {
        base.BeginInit ();

        if (Parent is null)
        {
            return;
        }

        ShadowStyle = base.ShadowStyle;
    }

    /// <summary>
    ///     The color scheme for the Margin. If set to <see langword="null"/> (the default), the margin will be transparent.
    /// </summary>
    public override ColorScheme? ColorScheme
    {
        get
        {
            if (base.ColorScheme is { })
            {
                return base.ColorScheme;
            }

            return (Parent?.SuperView?.ColorScheme ?? Colors.ColorSchemes ["TopLevel"])!;
        }
        set
        {
            base.ColorScheme = value;
            Parent?.SetNeedsDraw ();
        }
    }

    /// <inheritdoc/>
    protected override bool OnClearingViewport ()
    {
        if (Thickness == Thickness.Empty)
        {
            return true;
        }

        Rectangle screen = ViewportToScreen (Viewport);

        // This just draws/clears the thickness, not the insides.
        if (Diagnostics.HasFlag (ViewDiagnosticFlags.Thickness) || base.ColorScheme is { })
        {
            Thickness.Draw (screen, Diagnostics, ToString ());
        }

        if (ShadowStyle != ShadowStyle.None)
        {
            // Don't clear where the shadow goes
            screen = Rectangle.Inflate (screen, -SHADOW_WIDTH, -SHADOW_HEIGHT);
        }

        return true;
    }

    #region Shadow

    private bool _pressed;
    private ShadowView? _bottomShadow;
    private ShadowView? _rightShadow;

    /// <summary>
    ///     Sets whether the Margin includes a shadow effect. The shadow is drawn on the right and bottom sides of the
    ///     Margin.
    /// </summary>
    public ShadowStyle SetShadow (ShadowStyle style)
    {
        if (_rightShadow is { })
        {
            Remove (_rightShadow);
            _rightShadow.Dispose ();
            _rightShadow = null;
        }

        if (_bottomShadow is { })
        {
            Remove (_bottomShadow);
            _bottomShadow.Dispose ();
            _bottomShadow = null;
        }

        if (ShadowStyle != ShadowStyle.None)
        {
            // Turn off shadow
            Thickness = new (Thickness.Left, Thickness.Top, Thickness.Right - SHADOW_WIDTH, Thickness.Bottom - SHADOW_HEIGHT);
        }

        if (style != ShadowStyle.None)
        {
            // Turn on shadow
            Thickness = new (Thickness.Left, Thickness.Top, Thickness.Right + SHADOW_WIDTH, Thickness.Bottom + SHADOW_HEIGHT);
        }

        if (style != ShadowStyle.None)
        {
            _rightShadow = new ()
            {
                X = Pos.AnchorEnd (SHADOW_WIDTH),
                Y = 0,
                Width = SHADOW_WIDTH,
                Height = Dim.Fill (),
                ShadowStyle = style,
                Orientation = Orientation.Vertical
            };

            _bottomShadow = new ()
            {
                X = 0,
                Y = Pos.AnchorEnd (SHADOW_HEIGHT),
                Width = Dim.Fill (),
                Height = SHADOW_HEIGHT,
                ShadowStyle = style,
                Orientation = Orientation.Horizontal
            };
            Add (_rightShadow, _bottomShadow);
        }

        return style;
    }

    /// <inheritdoc/>
    public override ShadowStyle ShadowStyle
    {
        get => base.ShadowStyle;
        set => base.ShadowStyle = SetShadow (value);
    }

    private void Margin_Highlight (object? sender, CancelEventArgs<HighlightStyle> e)
    {
        if (Thickness == Thickness.Empty || ShadowStyle == ShadowStyle.None)
        {
            return;
        }

        if (_pressed && e.NewValue == HighlightStyle.None)
        {
            // If the view is pressed and the highlight is being removed, move the shadow back.
            // Note, for visual effects reasons, we only move horizontally.
            // TODO: Add a setting or flag that lets the view move vertically as well.
            Thickness = new (
                             Thickness.Left - PRESS_MOVE_HORIZONTAL,
                             Thickness.Top - PRESS_MOVE_VERTICAL,
                             Thickness.Right + PRESS_MOVE_HORIZONTAL,
                             Thickness.Bottom + PRESS_MOVE_VERTICAL);

            if (_rightShadow is { })
            {
                _rightShadow.Visible = true;
            }

            if (_bottomShadow is { })
            {
                _bottomShadow.Visible = true;
            }

            _pressed = false;

            return;
        }

        if (!_pressed && e.NewValue.HasFlag (HighlightStyle.Pressed))
        {
            // If the view is not pressed and we want highlight move the shadow
            // Note, for visual effects reasons, we only move horizontally.
            // TODO: Add a setting or flag that lets the view move vertically as well.
            Thickness = new (
                             Thickness.Left + PRESS_MOVE_HORIZONTAL,
                             Thickness.Top + PRESS_MOVE_VERTICAL,
                             Thickness.Right - PRESS_MOVE_HORIZONTAL,
                             Thickness.Bottom - PRESS_MOVE_VERTICAL);
            _pressed = true;

            if (_rightShadow is { })
            {
                _rightShadow.Visible = false;
            }

            if (_bottomShadow is { })
            {
                _bottomShadow.Visible = false;
            }
        }
    }

    private void Margin_LayoutStarted (object? sender, LayoutEventArgs e)
    {
        // Adjust the shadow such that it is drawn aligned with the Border
        if (_rightShadow is { } && _bottomShadow is { })
        {
            switch (ShadowStyle)
            {
                case ShadowStyle.Transparent:
                    // BUGBUG: This doesn't work right for all Border.Top sizes - Need an API on Border that gives top-right location of line corner.
                    _rightShadow.Y = Parent!.Border!.Thickness.Top > 0 ? ScreenToViewport (Parent.Border.GetBorderRectangle ().Location).Y + 1 : 0;

                    break;

                case ShadowStyle.Opaque:
                    // BUGBUG: This doesn't work right for all Border.Top sizes - Need an API on Border that gives top-right location of line corner.
                    _rightShadow.Y = Parent!.Border!.Thickness.Top > 0 ? ScreenToViewport (Parent.Border.GetBorderRectangle ().Location).Y + 1 : 0;
                    _bottomShadow.X = Parent.Border.Thickness.Left > 0 ? ScreenToViewport (Parent.Border.GetBorderRectangle ().Location).X + 1 : 0;

                    break;

                case ShadowStyle.None:
                default:
                    _rightShadow.Y = 0;
                    _bottomShadow.X = 0;

                    break;
            }
        }
    }

    #endregion Shadow

}
