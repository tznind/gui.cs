﻿#nullable enable
namespace Terminal.Gui.ViewBase;

public partial class View
{
    /// <summary>
    ///    Gets or sets the user actions that are enabled for the arranging this view within it's <see cref="SuperView"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    ///     See the View Arrangement Deep Dive for more information: <see href="https://gui-cs.github.io/Terminal.Gui/docs/arrangement.html"/>
    /// </para>
    /// </remarks>
    public ViewArrangement Arrangement { get; set; }
}
