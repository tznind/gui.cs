using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

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
        // For building test cases
        //Logging.Logger.LogTrace (FormatConsoleKeyInfoForTestCase (input));

        Key key = ConsoleKeyInfoToKey (input);
        OnKeyDown (key);
        OnKeyUp (key);
    }

    /// <summary>
    /// Converts terminal raw input class <see cref="ConsoleKeyInfo"/> into
    /// common Terminal.Gui event model for keypresses (<see cref="Key"/>)
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public static Key ConsoleKeyInfoToKey (ConsoleKeyInfo input)
    {
        ConsoleKeyInfo adjustedInput = EscSeqUtils.MapConsoleKeyInfo (input);

        // TODO : EscSeqUtils.MapConsoleKeyInfo is wrong for e.g. '{' - it winds up clearing the Key
        //        So if the method nuked it then we should just work with the original.
        if (adjustedInput.Key == ConsoleKey.None && input.Key != ConsoleKey.None)
        {
            return EscSeqUtils.MapKey (input);
        }

        return EscSeqUtils.MapKey (adjustedInput);
    }

    /* For building test cases
    public static string FormatConsoleKeyInfoForTestCase (ConsoleKeyInfo input)
    {
        string charLiteral = input.KeyChar == '\0' ? @"'\0'" : $"'{input.KeyChar}'";
        string expectedLiteral = $"new Rune('todo')";

        return $"yield return new object[] {{ new ConsoleKeyInfo({charLiteral}, ConsoleKey.{input.Key}, " +
               $"{input.Modifiers.HasFlag (ConsoleModifiers.Shift).ToString ().ToLower ()}, " +
               $"{input.Modifiers.HasFlag (ConsoleModifiers.Alt).ToString ().ToLower ()}, " +
               $"{input.Modifiers.HasFlag (ConsoleModifiers.Control).ToString ().ToLower ()}), {expectedLiteral} }};";
    }*/
}
