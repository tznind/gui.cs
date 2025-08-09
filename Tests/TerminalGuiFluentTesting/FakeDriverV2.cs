using System.Collections.Concurrent;
using System.Drawing;

namespace TerminalGuiFluentTesting;


public class FakeDriverFactory
{
    /// <summary>
    /// Creates a new instance of <see cref="FakeDriverV2"/> using default options
    /// </summary>
    /// <returns></returns>
    public FakeDriverV2 Create ()
    {
        return new FakeDriverV2 (
                                 new ConcurrentQueue<ConsoleKeyInfo> (),
                                 new OutputBuffer (),
                                 new FakeOutput (),
                                 () => DateTime.Now,
                                 new FakeSizeMonitor ());
    }
}

/// <summary>
/// Implementation of <see cref="IConsoleDriver"/> that uses fake input/output.
/// This is a lightweight alternative to <see cref="GuiTestContext"/> (if you don't
/// need the entire application main loop running).
/// </summary>
public class FakeDriverV2 : ConsoleDriverFacade<ConsoleKeyInfo>
{
    public ConcurrentQueue<ConsoleKeyInfo> InputBuffer { get; }
    public FakeSizeMonitor SizeMonitor { get; }
    public OutputBuffer OutputBuffer { get; }

    public IConsoleOutput ConsoleOutput { get; }

    private FakeOutput _fakeOutput;

    internal FakeDriverV2 (
        ConcurrentQueue<ConsoleKeyInfo> inputBuffer,
        OutputBuffer outputBuffer,
        FakeOutput fakeOutput,
        Func<DateTime> datetimeFunc,
        FakeSizeMonitor sizeMonitor) :
        base (new NetInputProcessor (inputBuffer),
             outputBuffer,
             fakeOutput,
             new (new AnsiResponseParser (), datetimeFunc),
             sizeMonitor)
    {
        InputBuffer = inputBuffer;
        SizeMonitor = sizeMonitor;
        OutputBuffer = outputBuffer;
        ConsoleOutput = _fakeOutput = fakeOutput;
        SizeChanged += (_, e) =>
                       {
                           if (e.Size != null)
                           {
                               _fakeOutput.Size = e.Size.Value;
                           }
                       };

    }
}

public class FakeSizeMonitor : IWindowSizeMonitor
{
    /// <inheritdoc />
    public event EventHandler<SizeChangedEventArgs>? SizeChanging;

    /// <inheritdoc />
    public bool Poll ()
    {
        return false;
    }

    /// <summary>
    /// Raises the <see cref="SizeChanging"/> event.
    /// </summary>
    /// <param name="newSize"></param>
    public void RaiseSizeChanging (Size newSize)
    {
        SizeChanging.Invoke (this,new (newSize));
    }
}
