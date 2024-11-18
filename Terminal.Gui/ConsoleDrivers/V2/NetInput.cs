using System.Collections.Concurrent;

namespace Terminal.Gui;

class NetInput (ConcurrentQueue<ConsoleKeyInfo> inputBuffer) : ConsoleInput<ConsoleKeyInfo> (inputBuffer)
{
    /// <inheritdoc />
    protected override bool Peek () => Console.KeyAvailable;

    /// <inheritdoc />
    protected override IEnumerable<ConsoleKeyInfo> Read ()
    {
        return [Console.ReadKey (true)];
    }
}