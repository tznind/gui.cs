#nullable enable
namespace Terminal.Gui;

/// <summary>
///     Abstract base class for Popover Views.
/// </summary>
/// <remarks>
///     <para>
///         To show a Popover, use <see cref="ApplicationPopover.Show"/>. To hide a popover,
///         call <see cref="ApplicationPopover.Show"/> with <see langword="null"/> set <see cref="View.Visible"/> to <see langword="false"/>.
///     </para>
///     <para>
///         If the user clicks anywhere not occluded by a SubView of the Popover, presses <see cref="Application.QuitKey"/>,
///         or causes another popover to show, the Popover will be hidden.
///     </para>
/// </remarks>
public abstract class PopoverBaseImpl : View, IPopover
{
    /// <summary>
    ///     Creates a new PopoverBaseImpl.
    /// </summary>
    protected PopoverBaseImpl ()
    {
        Id = "popoverBaseImpl";
        CanFocus = true;
        Width = Dim.Fill ();
        Height = Dim.Fill ();
        ViewportSettings = ViewportSettings.Transparent | ViewportSettings.TransparentMouse;

        // TODO: Add a diagnostic setting for this?
        //TextFormatter.VerticalAlignment = Alignment.End;
        //TextFormatter.Alignment = Alignment.End;
        //base.Text = "popover";

        AddCommand (Command.Quit, Quit);
        KeyBindings.Add (Application.QuitKey, Command.Quit);

        return;

        bool? Quit (ICommandContext? ctx)
        {
            if (!Visible)
            {
                return null;
            }

            Visible = false;

            return true;
        }
    }

    /// <inheritdoc />
    protected override bool OnVisibleChanging ()
    {
        bool ret = base.OnVisibleChanging ();
        if (!ret && !Visible)
        {
            // Whenever visible is changing to true, we need to resize;
            // it's our only chance because we don't get laid out until we're visible
            Layout (Application.Screen.Size);
        }

        return ret;
    }
}
