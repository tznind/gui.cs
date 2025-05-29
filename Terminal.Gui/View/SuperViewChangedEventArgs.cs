﻿namespace Terminal.Gui;

/// <summary>
///     Args for events where the <see cref="View.SuperView"/> of a <see cref="View"/> is changed (e.g.
///     <see cref="View.Removed"/> / <see cref="View.IsAddedChanged"/> events).
/// </summary>
public class SuperViewChangedEventArgs : EventArgs
{
    /// <summary>Creates a new instance of the <see cref="SuperViewChangedEventArgs"/> class.</summary>
    /// <param name="superView"></param>
    /// <param name="subView"></param>
    public SuperViewChangedEventArgs (View superView, View subView)
    {
        SuperView = superView;
        SubView = subView;
    }

    /// <summary>The view that is having it's <see cref="View.SuperView"/> changed</summary>
    public View SubView { get; }

    /// <summary>
    ///     The parent.  For <see cref="View.Removed"/> this is the old parent (new parent now being null).  For
    ///     <see cref="View.IsAddedChanged"/> it is the new parent to whom view now belongs.
    /// </summary>
    public View SuperView { get; }
}
