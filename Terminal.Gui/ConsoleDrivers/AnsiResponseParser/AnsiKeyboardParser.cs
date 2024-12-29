using System.Text.RegularExpressions;

namespace Terminal.Gui;

/// <summary>
/// Parses ansi escape sequence strings that describe keyboard activity e.g. cursor keys
/// into <see cref="Key"/>.
/// </summary>
public class AnsiKeyboardParser
{
    // Regex patterns for ANSI arrow keys (Up, Down, Left, Right)
    private readonly Regex _arrowKeyPattern = new (@"\u001b\[(A|B|C|D)", RegexOptions.Compiled);

    /// <summary>
    ///     Parses an ANSI escape sequence into a keyboard event. Returns null if input
    ///     is not a recognized keyboard event or its syntax is not understood.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public Key ProcessKeyboardInput (string input)
    {
        // Match arrow key events
        Match match = _arrowKeyPattern.Match (input);

        if (match.Success)
        {
            char direction = match.Groups [1].Value [0];

            return direction switch
                   {
                       'A' => Key.CursorUp,
                       'B' => Key.CursorDown,
                       'C' => Key.CursorRight,
                       'D' => Key.CursorDown,
                       _ => default (Key)
                   };
        }

        // It's an unrecognized keyboard event
        return null;
    }

    /// <summary>
    /// Returns <see langword="true"/> if the given escape code
    /// is a keyboard escape code (e.g. cursor key)
    /// </summary>
    /// <param name="cur">escape code</param>
    /// <returns></returns>
    public bool IsKeyboard (string cur) { return _arrowKeyPattern.IsMatch (cur); }
}
