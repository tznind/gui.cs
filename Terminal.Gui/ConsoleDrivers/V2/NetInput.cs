using System.Collections.Concurrent;

namespace Terminal.Gui;

public class NetInput : ConsoleInput<ConsoleKeyInfo>
{
    /// <inheritdoc />
    protected override bool Peek () => Console.KeyAvailable;

    /// <inheritdoc />
    protected override IEnumerable<ConsoleKeyInfo> Read ()
    {
        return [Console.ReadKey (true)];
    }
}