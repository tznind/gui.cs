using System.Collections.Concurrent;
using Terminal.Gui.ConsoleDrivers;
using InputRecord = Terminal.Gui.WindowsConsole.InputRecord;

namespace UnitTests.ConsoleDrivers.V2;
public class WindowsInputProcessorTests
{

    [Fact]
    public void Test_ProcessQueue_CapitalHLowerE ()
    {
        var queue = new ConcurrentQueue<InputRecord> ();

        queue.Enqueue (new  InputRecord()
        {
            EventType = WindowsConsole.EventType.Key,
            KeyEvent = new WindowsConsole.KeyEventRecord ()
            {
                bKeyDown = true,
                UnicodeChar = 'H',
                dwControlKeyState = WindowsConsole.ControlKeyState.CapslockOn,
                wVirtualKeyCode = (ConsoleKeyMapping.VK)72,
                wVirtualScanCode = 35
            }
        });
        queue.Enqueue (new InputRecord ()
        {
            EventType = WindowsConsole.EventType.Key,
            KeyEvent = new WindowsConsole.KeyEventRecord ()
            {
                bKeyDown = true,
                UnicodeChar = 'i',
                dwControlKeyState = WindowsConsole.ControlKeyState.NoControlKeyPressed,
                wVirtualKeyCode = (ConsoleKeyMapping.VK)73,
                wVirtualScanCode = 23
            }
        });

        var processor = new WindowsInputProcessor (queue);

        List<Key> ups = new List<Key> ();
        List<Key> downs = new List<Key> ();

        processor.KeyUp += (s, e) => { ups.Add (e); };
        processor.KeyDown += (s, e) => { downs.Add (e); };

        Assert.Empty (ups);
        Assert.Empty (downs);

        processor.ProcessQueue ();

        Assert.Equal (Key.H.WithShift, ups [0]);
        Assert.Equal (Key.H.WithShift, downs [0]);
        Assert.Equal (Key.I, ups [1]);
        Assert.Equal (Key.I, downs [1]);
    }
}

