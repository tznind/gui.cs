using System.Collections.Concurrent;

namespace Terminal.Gui;

/// <summary>
///     Input processor for <see cref="NetInput"/>, deals in <see cref="ConsoleKeyInfo"/> stream
/// </summary>
public class NetInputProcessor : InputProcessor<ConsoleKeyInfo>
{
    /// <inheritdoc/>
    public NetInputProcessor (ConcurrentQueue<ConsoleKeyInfo> inputBuffer) : base (inputBuffer) { }

    /// <inheritdoc/>
    protected override void Process (ConsoleKeyInfo consoleKeyInfo)
    {
        foreach (Tuple<char, ConsoleKeyInfo> released in Parser.ProcessInput (Tuple.Create (consoleKeyInfo.KeyChar, consoleKeyInfo)))
        {
            ProcessAfterParsing (released.Item2);
        }
    }

    /// <inheritdoc/>
    protected override void ProcessAfterParsing (ConsoleKeyInfo input)
    {
        ConsoleKeyInfo adjustedInput = EscSeqUtils.MapConsoleKeyInfo (input);
        
        KeyCode key;

        // TODO : EscSeqUtils.MapConsoleKeyInfo is wrong for e.g. '{' - it winds up clearing the Key
        //        So if the method nuked it then we should just work with the original.
        if (adjustedInput.Key == ConsoleKey.None && input.Key != ConsoleKey.None)
        {
            key = EscSeqUtils.MapKey (input);
        }
        else
        {
            key = EscSeqUtils.MapKey (adjustedInput);
        }
        OnKeyDown (key);
        OnKeyUp (key);
    }
}
