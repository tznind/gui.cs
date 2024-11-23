using System.Collections.Concurrent;
using static Terminal.Gui.ConsoleDrivers.ConsoleKeyMapping;
using static Terminal.Gui.WindowsConsole;

namespace Terminal.Gui;
using InputRecord = InputRecord;

/// <summary>
/// Input processor for <see cref="WindowsInput"/>, deals in <see cref="WindowsConsole.InputRecord"/> stream.
/// </summary>
public class WindowsInputProcessor : InputProcessor<InputRecord>
{
    /// <inheritdoc />
    public WindowsInputProcessor (ConcurrentQueue<InputRecord> inputBuffer) : base (inputBuffer) { }

    /// <inheritdoc />
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
                    var key = (Key) (released.Item2.KeyEvent.UnicodeChar);
                    OnKeyDown (key);
                    OnKeyUp (key);
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

    private MouseEventArgs ToDriverMouse (MouseEventRecord e)
    {
        var result = new MouseEventArgs
        {
            Position = new (e.MousePosition.X, e.MousePosition.Y)
        };

        // TODO: Return keys too

        return result;
    }
}
