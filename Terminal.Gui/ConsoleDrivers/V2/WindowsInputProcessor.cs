using System.Collections.Concurrent;
using static Terminal.Gui.ConsoleDrivers.ConsoleKeyMapping;

namespace Terminal.Gui.ConsoleDrivers.V2;

/// <summary>
/// Input processor for <see cref="WindowsInput"/>, deals in <see cref="WindowsConsole.InputRecord"/> stream.
/// </summary>
public class WindowsInputProcessor : InputProcessor<WindowsConsole.InputRecord>
{
    /// <inheritdoc />
    public WindowsInputProcessor (ConcurrentQueue<WindowsConsole.InputRecord> inputBuffer) : base (inputBuffer) { }

    /// <inheritdoc />
    protected override void Process (WindowsConsole.InputRecord inputEvent)
    {
        switch (inputEvent.EventType)
        {

            case WindowsConsole.EventType.Key:

                var mapped = (Key)inputEvent.KeyEvent.UnicodeChar;
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

                if (inputEvent.KeyEvent.bKeyDown)
                {
                    OnKeyDown (mapped);
                }
                else
                {
                    OnKeyUp (mapped);
                }

                break;

            case WindowsConsole.EventType.Mouse:
                MouseEventArgs me = ToDriverMouse (inputEvent.MouseEvent);

                OnMouseEvent (me);

                break;
        }
    }

    private MouseEventArgs ToDriverMouse (WindowsConsole.MouseEventRecord e)
    {
        var result = new MouseEventArgs ()
        {
            Position = new (e.MousePosition.X, e.MousePosition.Y)
        };

        // TODO: Return keys too

        return result;
    }
}
