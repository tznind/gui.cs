﻿#nullable enable
using System.ComponentModel;

namespace Terminal.Gui.ViewBase;

public partial class View
{
    private string? _schemeName;

    /// <summary>
    ///     Gets or sets the name of the scheme to use for this <see cref="View"/>. If set, it overrides the scheme
    ///     inherited from the <see cref="SuperView"/>. If a scheme was explicitly set (<see cref="HasScheme"/> is
    ///     true), this property is ignored.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Setting this property raises pre- and post-change events via <see cref="CWPPropertyHelper"/>,
    ///         allowing customization or cancellation of the change. The <see cref="SchemeNameChanging"/> event
    ///         is raised before the change, and <see cref="SchemeNameChanged"/> is raised after.
    ///     </para>
    /// </remarks>
    /// <value>The scheme name, or null if no scheme name is set.</value>
    /// <seealso cref="SchemeNameChanging"/>
    /// <seealso cref="SchemeNameChanged"/>
    public string? SchemeName
    {
        get => _schemeName;
        set
        {
            bool changed = CWPPropertyHelper.ChangeProperty (
                _schemeName,
                value,
                OnSchemeNameChanging,
                SchemeNameChanging,
                OnSchemeNameChanged,
                SchemeNameChanged,
                out string? finalValue);

            if (changed)
            {
                _schemeName = finalValue;
            }
        }
    }

    /// <summary>
    ///     Called before the <see cref="SchemeName"/> property changes, allowing subclasses to cancel or modify the change.
    /// </summary>
    /// <param name="args">The event arguments containing the current and proposed new scheme name.</param>
    /// <returns>True to cancel the change, false to proceed.</returns>
    protected virtual bool OnSchemeNameChanging (ValueChangingEventArgs<string?> args)
    {
        return false;
    }

    /// <summary>
    ///     Called after the <see cref="SchemeName"/> property changes, allowing subclasses to react to the change.
    /// </summary>
    /// <param name="args">The event arguments containing the old and new scheme name.</param>
    protected virtual void OnSchemeNameChanged (ValueChangedEventArgs<string?> args)
    {
    }

    /// <summary>
    ///     Raised before the <see cref="SchemeName"/> property changes, allowing handlers to modify or cancel the change.
    /// </summary>
    /// <remarks>
    ///     Set <see cref="ValueChangingEventArgs{T}.Handled"/> to true to cancel the change or modify
    ///     <see cref="ValueChangingEventArgs{T}.NewValue"/> to adjust the proposed value.
    /// </remarks>
    /// <example>
    ///     <code>
    ///         view.SchemeNameChanging += (sender, args) =>
    ///         {
    ///             if (args.NewValue == "InvalidScheme")
    ///             {
    ///                 args.Handled = true;
    ///                 Console.WriteLine("Invalid scheme name cancelled.");
    ///             }
    ///         };
    ///     </code>
    /// </example>
    public event EventHandler<ValueChangingEventArgs<string?>>? SchemeNameChanging;

    /// <summary>
    ///     Raised after the <see cref="SchemeName"/> property changes, notifying handlers of the completed change.
    /// </summary>
    /// <remarks>
    ///     Provides the old and new scheme name via <see cref="ValueChangedEventArgs{T}.OldValue"/> and
    ///     <see cref="ValueChangedEventArgs{T}.NewValue"/>, which may be null.
    /// </remarks>
    /// <example>
    ///     <code>
    ///         view.SchemeNameChanged += (sender, args) =>
    ///         {
    ///             Console.WriteLine($"SchemeName changed from {args.OldValue ?? "none"} to {args.NewValue ?? "none"}.");
    ///         };
    ///     </code>
    /// </example>
    public event EventHandler<ValueChangedEventArgs<string?>>? SchemeNameChanged;

    // Both holds the set Scheme and is used to determine if a Scheme has been set or not
    private Scheme? _scheme;

    /// <summary>
    ///     Gets whether a Scheme has been explicitly set for this View, or if it will inherit the Scheme from its
    ///     <see cref="SuperView"/>.
    /// </summary>
    public bool HasScheme => _scheme is { };

    /// <summary>
    ///     Gets the scheme for the <see cref="View"/>. If the scheme has not been explicitly set
    ///     (see <see cref="HasScheme"/>), gets the <see cref="SuperView"/>'s scheme or falls back to the base scheme.
    /// </summary>
    /// <returns>The resolved scheme, never null.</returns>
    /// <remarks>
    ///     <para>
    ///         This method uses the Cancellable Work Pattern (CWP) via <see cref="CWPWorkflowHelper.ExecuteWithResult{TResult}"/>
    ///         to allow customization or cancellation of scheme resolution through the <see cref="OnGettingScheme"/> method
    ///         and <see cref="GettingScheme"/> event.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    ///         view.GettingScheme += (sender, args) =>
    ///         {
    ///             args.Result = SchemeManager.GetScheme("Custom");
    ///             args.Handled = true;
    ///         };
    ///         Scheme scheme = view.GetScheme();
    ///     </code>
    /// </example>
    public Scheme GetScheme ()
    {
        ResultEventArgs<Scheme?> args = new ();

        return CWPWorkflowHelper.ExecuteWithResult (
                                                    onMethod: args =>
                                                              {
                                                                  bool cancelled = OnGettingScheme (out Scheme? newScheme);
                                                                  args.Result = newScheme;
                                                                  return cancelled;
                                                              },
                                                    eventHandler: GettingScheme,
                                                    args,
                                                    DefaultAction);

        Scheme DefaultAction ()
        {
            if (!HasScheme && !string.IsNullOrEmpty (SchemeName))
            {
                return SchemeManager.GetScheme (SchemeName);
            }

            if (!HasScheme)
            {
                return SuperView?.GetScheme () ?? SchemeManager.GetScheme (Schemes.Base);
            }

            return _scheme!;
        }
    }

    /// <summary>
    ///     Called when the <see cref="Scheme"/> for the <see cref="View"/> is being retrieved. Subclasses can return
    ///     true to stop further processing and optionally set <paramref name="scheme"/> to a different value.
    /// </summary>
    /// <param name="scheme">The scheme to use, or null to continue processing.</param>
    /// <returns>True to stop default behavior, false to proceed.</returns>
    protected virtual bool OnGettingScheme (out Scheme? scheme)
    {
        scheme = null;
        return false;
    }

    /// <summary>
    ///     Raised when the <see cref="Scheme"/> for the <see cref="View"/> is being retrieved. Handlers can set
    ///     <see cref="ResultEventArgs{T}.Handled"/> to true to stop further processing and optionally set
    ///     <see cref="ResultEventArgs{T}.Result"/> to a different value.
    /// </summary>
    public event EventHandler<ResultEventArgs<Scheme?>>? GettingScheme;


    /// <summary>
    ///     Sets the scheme for the <see cref="View"/>, marking it as explicitly set.
    /// </summary>
    /// <param name="scheme">The scheme to set, or null to clear the explicit scheme.</param>
    /// <returns>True if the scheme was set, false if unchanged or cancelled.</returns>
    /// <remarks>
    ///     <para>
    ///         This method uses the Cancellable Work Pattern (CWP) via <see cref="CWPPropertyHelper.ChangeProperty{T}"/>
    ///         to allow customization or cancellation of the scheme change through the <see cref="OnSettingScheme"/> method
    ///         and <see cref="SchemeChanging"/> event. The <see cref="SchemeChanged"/> event is raised after a successful change.
    ///     </para>
    ///     <para>
    ///         If set to null, <see cref="HasScheme"/> will be false, and the view will inherit the scheme from its
    ///         <see cref="SuperView"/> or fall back to the base scheme.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    ///         view.SchemeChanging += (sender, args) =>
    ///         {
    ///             if (args.NewValue is null)
    ///             {
    ///                 args.Handled = true;
    ///                 Console.WriteLine("Null scheme cancelled.");
    ///             }
    ///         };
    ///         view.SchemeChanged += (sender, args) =>
    ///         {
    ///             Console.WriteLine($"Scheme changed to {args.NewValue?.Name ?? "none"}.");
    ///         };
    ///         bool set = view.SetScheme(SchemeManager.GetScheme("Base"));
    ///     </code>
    /// </example>
    public bool SetScheme (Scheme? scheme)
    {
        bool changed = CWPPropertyHelper.ChangeProperty (
            _scheme,
            scheme,
            OnSettingScheme,
            SchemeChanging,
            OnSchemeChanged,
            SchemeChanged,
            out Scheme? finalValue);

        if (changed)
        {
            _scheme = finalValue;
            return true;
        }
        return false;
    }

    /// <summary>
    ///     Called before the scheme is set, allowing subclasses to cancel or modify the change.
    /// </summary>
    /// <param name="args">The event arguments containing the current and proposed new scheme.</param>
    /// <returns>True to cancel the change, false to proceed.</returns>
    protected virtual bool OnSettingScheme (ValueChangingEventArgs<Scheme?> args)
    {
        return false;
    }

    /// <summary>
    ///     Called after the scheme is set, allowing subclasses to react to the change.
    /// </summary>
    /// <param name="args">The event arguments containing the old and new scheme.</param>
    protected virtual void OnSchemeChanged (ValueChangedEventArgs<Scheme?> args)
    {
        SetNeedsDraw ();
    }

    /// <summary>
    ///     Raised before the scheme is set, allowing handlers to modify or cancel the change.
    /// </summary>
    /// <remarks>
    ///     Set <see cref="ValueChangingEventArgs{T}.Handled"/> to true to cancel the change or modify
    ///     <see cref="ValueChangingEventArgs{T}.NewValue"/> to adjust the proposed scheme.
    /// </remarks>
    public event EventHandler<ValueChangingEventArgs<Scheme?>>? SchemeChanging;

    /// <summary>
    ///     Raised after the scheme is set, notifying handlers of the completed change.
    /// </summary>
    /// <remarks>
    ///     Provides the old and new scheme via <see cref="ValueChangedEventArgs{T}.OldValue"/> and
    ///     <see cref="ValueChangedEventArgs{T}.NewValue"/>, which may be null.
    /// </remarks>
    public event EventHandler<ValueChangedEventArgs<Scheme?>>? SchemeChanged;

}
