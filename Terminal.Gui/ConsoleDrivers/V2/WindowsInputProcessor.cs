using System.Collections.Concurrent;

namespace Terminal.Gui.ConsoleDrivers.V2;

/// <summary>
/// Input processor for <see cref="WindowsInput"/>, deals in <see cref="WindowsConsole.InputRecord"/> stream.
/// </summary>
public class WindowsInputProcessor : InputProcessor<WindowsConsole.InputRecord>
{
    /// <inheritdoc />
    public WindowsInputProcessor (ConcurrentQueue<WindowsConsole.InputRecord> inputBuffer) : base (inputBuffer) { }

    /// <inheritdoc />
    protected override void Process (WindowsConsole.InputRecord result)
    {

    }
}
