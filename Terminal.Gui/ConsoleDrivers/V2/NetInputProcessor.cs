using System.Collections.Concurrent;
using Terminal.Gui.ConsoleDrivers;

namespace Terminal.Gui;

/// <summary>
/// Input processor for <see cref="NetInput"/>, deals in <see cref="ConsoleKeyInfo"/> stream
/// </summary>
public class NetInputProcessor : InputProcessor<ConsoleKeyInfo>
{
    /// <inheritdoc />
    public NetInputProcessor (ConcurrentQueue<ConsoleKeyInfo> inputBuffer) : base (inputBuffer) { }

    /// <inheritdoc />
    protected override void Process (ConsoleKeyInfo consoleKeyInfo)
    {
        foreach (Tuple<char, ConsoleKeyInfo> released in Parser.ProcessInput (Tuple.Create (consoleKeyInfo.KeyChar, consoleKeyInfo)))
        {
            ProcessAfterParsing (released.Item2);
        }
    }

    /// <inheritdoc />
    protected override void ProcessAfterParsing (ConsoleKeyInfo input)
    {
        var key = ConsoleKeyMapping.MapKey (input);
        OnKeyDown (key);
        OnKeyUp (key);
    }
}
