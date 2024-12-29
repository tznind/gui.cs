using System.Text.RegularExpressions;

namespace Terminal.Gui;

/// <summary>
/// Parses ansi escape sequence strings that describe keyboard activity e.g. cursor keys
/// into <see cref="Key"/>.
/// </summary>
public class AnsiKeyboardParser
{
    // Regex patterns for ANSI arrow keys (Up, Down, Left, Right)
    private readonly Regex _arrowKeyPattern = new (@"^\u001b\[(1;(\d+))?([A-D])$", RegexOptions.Compiled);

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
            // Group 2 captures the modifier number, if present
            string modifierGroup = match.Groups [2].Value;
            char direction = match.Groups [3].Value [0];

            Key key = direction switch
                   {
                       'A' => Key.CursorUp,
                       'B' => Key.CursorDown,
                       'C' => Key.CursorRight,
                       'D' => Key.CursorLeft,
                       _ => default (Key)
                   };

            if(key == null)
            {
                return null;
            }

            // TODO: these are wrong I think
            // Apply modifiers based on the modifier number
            if (modifierGroup == "3") // Ctrl
            {
                key = key.WithCtrl;
            }
            else if (modifierGroup == "4") // Alt
            {
                key = key.WithAlt;
            }
            else if (modifierGroup == "5") // Shift
            {
                key = key.WithShift;
            }

            return key;
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
