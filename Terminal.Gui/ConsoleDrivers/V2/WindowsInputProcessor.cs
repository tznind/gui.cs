#nullable enable
using System.Collections.Concurrent;
using static Terminal.Gui.ConsoleDrivers.ConsoleKeyMapping;
using static Terminal.Gui.WindowsConsole;

namespace Terminal.Gui;

using InputRecord = InputRecord;

/// <summary>
///     Input processor for <see cref="WindowsInput"/>, deals in <see cref="WindowsConsole.InputRecord"/> stream.
/// </summary>
internal class WindowsInputProcessor : InputProcessor<InputRecord>
{
    /// <inheritdoc/>
    public WindowsInputProcessor (ConcurrentQueue<InputRecord> inputBuffer) : base (inputBuffer) { }

    /// <inheritdoc/>
    protected override void Process (InputRecord inputEvent)
    {
        switch (inputEvent.EventType)
        {
            case EventType.Key:

                // TODO: For now ignore keyup because ANSI comes in as down+up which is confusing to try and parse/pair these things up
                if (!inputEvent.KeyEvent.bKeyDown)
                {
                    return;
                }

                foreach (Tuple<char, InputRecord> released in Parser.ProcessInput (Tuple.Create (inputEvent.KeyEvent.UnicodeChar, inputEvent)))
                {
                    ProcessAfterParsing (released.Item2);
                }

                /*
                if (inputEvent.KeyEvent.wVirtualKeyCode == (VK)ConsoleKey.Packet)
                {
                    // Used to pass Unicode characters as if they were keystrokes.
                    // The VK_PACKET key is the low word of a 32-bit
                    // Virtual Key value used for non-keyboard input methods.
                    inputEvent.KeyEvent = FromVKPacketToKeyEventRecord (inputEvent.KeyEvent);
                }

                WindowsConsole.ConsoleKeyInfoEx keyInfo = ToConsoleKeyInfoEx (inputEvent.KeyEvent);

                //Debug.WriteLine ($"event: KBD: {GetKeyboardLayoutName()} {inputEvent.ToString ()} {keyInfo.ToString (keyInfo)}");

                KeyCode map = MapKey (keyInfo);

                if (map == KeyCode.Null)
                {
                    break;
                }
                */
                // This follows convention in NetDriver

                break;

            case EventType.Mouse:
                MouseEventArgs me = ToDriverMouse (inputEvent.MouseEvent);

                OnMouseEvent (me);

                break;
        }
    }

    /// <inheritdoc/>
    protected override void ProcessAfterParsing (InputRecord input)
    {
        // TODO: This should be in a shared helper method not calling statics in WindowsDriver
        if(InputRecordToKey (input, out var key))
        {
            OnKeyDown (key!);
            OnKeyUp (key!);
        }
    }

    private bool InputRecordToKey (InputRecord inputEvent, out Key? key)
    {
        if (inputEvent.KeyEvent.wVirtualKeyCode == (VK)ConsoleKey.Packet)
        {
            // Used to pass Unicode characters as if they were keystrokes.
            // The VK_PACKET key is the low word of a 32-bit
            // Virtual Key value used for non-keyboard input methods.
            inputEvent.KeyEvent = WindowsDriver.FromVKPacketToKeyEventRecord (inputEvent.KeyEvent);
        }

        WindowsConsole.ConsoleKeyInfoEx keyInfo = WindowsDriver.ToConsoleKeyInfoEx (inputEvent.KeyEvent);

        //Debug.WriteLine ($"event: KBD: {GetKeyboardLayoutName()} {inputEvent.ToString ()} {keyInfo.ToString (keyInfo)}");

        KeyCode map = WindowsDriver.MapKey (keyInfo);

        if (map == KeyCode.Null)
        {
            key = null;
            return false;
        }

        key = new Key (map);
        return true;
    }

    private MouseEventArgs ToDriverMouse (MouseEventRecord e)
    {
        var result = new MouseEventArgs
        {
            Position = new (e.MousePosition.X, e.MousePosition.Y),

            Flags = e.ButtonState switch
                    {
                        ButtonState.NoButtonPressed => MouseFlags.None,
                        ButtonState.Button1Pressed => MouseFlags.Button1Pressed,
                        ButtonState.Button2Pressed => MouseFlags.Button2Pressed,
                        ButtonState.Button3Pressed => MouseFlags.Button3Pressed,
                        ButtonState.Button4Pressed => MouseFlags.Button4Pressed,
                        ButtonState.RightmostButtonPressed => MouseFlags.Button3Pressed,
                        _=> MouseFlags.None
                    }
        };

        if (e.EventFlags == WindowsConsole.EventFlags.MouseWheeled)
        {
            switch ((int)e.ButtonState)
            {
                case int v when v > 0:
                    result.Flags = MouseFlags.WheeledUp;

                    break;

                case int v when v < 0:
                    result.Flags = MouseFlags.WheeledDown;

                    break;
            }
        }

        // TODO: Return keys too

        return result;
    }
}
