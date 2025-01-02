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
        Key key;

        if (input.KeyEvent.UnicodeChar == '\0')
        {
            key = input.KeyEvent.wVirtualKeyCode switch
                  {
                      VK.DOWN => Key.CursorDown,
                      VK.UP => Key.CursorUp,
                      VK.LEFT => Key.CursorLeft,
                      VK.RIGHT => Key.CursorRight,
                      VK.BACK => Key.Backspace,
                      VK.TAB => Key.Tab,
                      VK.F1 => Key.F1,
                      VK.F2 => Key.F2,
                      VK.F3 => Key.F3,
                      VK.F4 => Key.F4,
                      VK.F5 => Key.F5,
                      VK.F6 => Key.F6,
                      VK.F7 => Key.F7,
                      VK.F8 => Key.F8,
                      VK.F9 => Key.F9,
                      VK.F10 => Key.F10,
                      VK.F11 => Key.F11,
                      VK.F12 => Key.F12,
                      VK.F13 => Key.F13,
                      VK.F14 => Key.F14,
                      VK.F15 => Key.F15,
                      VK.F16 => Key.F16,
                      VK.F17 => Key.F17,
                      VK.F18 => Key.F18,
                      VK.F19 => Key.F19,
                      VK.F20 => Key.F20,
                      VK.F21 => Key.F21,
                      VK.F22 => Key.F22,
                      VK.F23 => Key.F23,
                      VK.F24 => Key.F24,
                      _ => '\0'
                  };
        }
        else
        {
            key = input.KeyEvent.UnicodeChar;
        }

        OnKeyDown (key);
        OnKeyUp (key);
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

                    } 
        };

        // TODO: Return keys too

        return result;
    }
}
