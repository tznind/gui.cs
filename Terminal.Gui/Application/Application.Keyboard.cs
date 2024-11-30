#nullable enable
namespace Terminal.Gui;

public static partial class Application // Keyboard handling
{
    /// <summary>
    ///     Called when the user presses a key (by the <see cref="IConsoleDriver"/>). Raises the cancelable
    ///     <see cref="KeyDown"/> event, then calls <see cref="View.NewKeyDownEvent"/> on all top level views, and finally
    ///     if the key was not handled, invokes any Application-scoped <see cref="KeyBindings"/>.
    /// </summary>
    /// <remarks>Can be used to simulate key press events.</remarks>
    /// <param name="key"></param>
    /// <returns><see langword="true"/> if the key was handled.</returns>
    public static bool RaiseKeyDownEvent (Key key)
    {
        KeyDown?.Invoke (null, key);

        if (key.Handled)
        {
            return true;
        }

        if (Top is null)
        {
            foreach (Toplevel topLevel in TopLevels.ToList ())
            {
                if (topLevel.NewKeyDownEvent (key))
                {
                    return true;
                }

                if (topLevel.Modal)
                {
                    break;
                }
            }
        }
        else
        {
            if (Top.NewKeyDownEvent (key))
            {
                return true;
            }
        }

        // Invoke any Application-scoped KeyBindings.
        // The first view that handles the key will stop the loop.
        foreach (KeyValuePair<Key, KeyBinding> binding in KeyBindings.Bindings.Where (b => b.Key == key.KeyCode))
        {
            if (binding.Value.BoundView is { })
            {
                if (!binding.Value.BoundView.Enabled)
                {
                    return false;
                }

                bool? handled = binding.Value.BoundView?.InvokeCommands (binding.Value.Commands, binding.Key, binding.Value);

                if (handled != null && (bool)handled)
                {
                    return true;
                }
            }
            else
            {
                if (!KeyBindings.TryGet (key, KeyBindingScope.Application, out KeyBinding appBinding))
                {
                    continue;
                }

                bool? toReturn = null;

                foreach (Command command in appBinding.Commands)
                {
                    toReturn = InvokeCommand (command, key, appBinding);
                }

                return toReturn ?? true;
            }
        }

        return false;

        static bool? InvokeCommand (Command command, Key key, KeyBinding appBinding)
        {
            if (!CommandImplementations!.ContainsKey (command))
            {
                throw new NotSupportedException (
                                                 @$"A KeyBinding was set up for the command {command} ({key}) but that command is not supported by Application."
                                                );
            }

            if (CommandImplementations.TryGetValue (command, out View.CommandImplementation? implementation))
            {
                var context = new CommandContext (command, key, appBinding); // Create the context here

                return implementation (context);
            }

            return false;
        }
    }

    /// <summary>
    ///     Raised when the user presses a key.
    ///     <para>
    ///         Set <see cref="Key.Handled"/> to <see langword="true"/> to indicate the key was handled and to prevent
    ///         additional processing.
    ///     </para>
    /// </summary>
    /// <remarks>
    ///     All drivers support firing the <see cref="KeyDown"/> event. Some drivers (Curses) do not support firing the
    ///     <see cref="KeyDown"/> and <see cref="KeyUp"/> events.
    ///     <para>Fired after <see cref="KeyDown"/> and before <see cref="KeyUp"/>.</para>
    /// </remarks>
    public static event EventHandler<Key>? KeyDown;

    /// <summary>
    ///     Called when the user releases a key (by the <see cref="IConsoleDriver"/>). Raises the cancelable <see cref="KeyUp"/>
    ///     event
    ///     then calls <see cref="View.NewKeyUpEvent"/> on all top level views. Called after <see cref="RaiseKeyDownEvent"/>.
    /// </summary>
    /// <remarks>Can be used to simulate key release events.</remarks>
    /// <param name="key"></param>
    /// <returns><see langword="true"/> if the key was handled.</returns>
    public static bool RaiseKeyUpEvent (Key key)
    {
        if (!Initialized)
        {
            return true;
        }

        KeyUp?.Invoke (null, key);

        if (key.Handled)
        {
            return true;
        }

        foreach (Toplevel topLevel in TopLevels.ToList ())
        {
            if (topLevel.NewKeyUpEvent (key))
            {
                return true;
            }

            if (topLevel.Modal)
            {
                break;
            }
        }

        return false;
    }

    #region Application-scoped KeyBindings


    /// <summary>Gets the Application-scoped key bindings.</summary>
    public static KeyBindings KeyBindings
    {
        get => ApplicationImpl.Instance.KeyBindings;
        set => ApplicationImpl.Instance.KeyBindings = value;
    }

    /// <summary>
    ///     Gets the list of Views that have <see cref="KeyBindingScope.Application"/> key bindings.
    /// </summary>
    /// <remarks>
    ///     This is an internal method used by the <see cref="View"/> class to add Application key bindings.
    /// </remarks>
    /// <returns>The list of Views that have Application-scoped key bindings.</returns>
    internal static List<KeyBinding> GetViewKeyBindings ()
    {
        // Get the list of views that do not have Application-scoped key bindings
        return KeyBindings.Bindings
                          .Where (kv => kv.Value.Scope != KeyBindingScope.Application)
                          .Select (kv => kv.Value)
                          .Distinct ()
                          .ToList ();
    }

    private static void ReplaceKey (Key oldKey, Key newKey)
    {
        if (KeyBindings.Bindings.Count == 0)
        {
            return;
        }

        if (newKey == Key.Empty)
        {
            KeyBindings.Remove (oldKey);
        }
        else
        {
            if (KeyBindings.TryGet(oldKey, out KeyBinding binding))
            {
                KeyBindings.Remove (oldKey);
                KeyBindings.Add (newKey, binding);
            }
            else
            {
                KeyBindings.Add (newKey, binding);
            }
        }
    }


    #endregion Application-scoped KeyBindings


}
