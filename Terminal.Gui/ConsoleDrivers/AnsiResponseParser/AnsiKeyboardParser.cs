#nullable enable
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace Terminal.Gui;

/// <summary>
/// Parses ansi escape sequence strings that describe keyboard activity e.g. cursor keys
/// into <see cref="Key"/>.
/// </summary>
public class AnsiKeyboardParser
{
    /*
     *F1	\u001bOP
       F2	\u001bOQ
       F3	\u001bOR
       F4	\u001bOS
       F5 (sometimes)	\u001bOt
       Left Arrow	\u001bOD
       Right Arrow	\u001bOC
       Up Arrow	\u001bOA
       Down Arrow	\u001bOB
     */
    private readonly Regex _ss3Pattern = new (@"^\u001bO([PQRStDCAB])$");

    /*
     * F1 - F12
     */
    private readonly Regex _functionKey = new (@"^\u001b\[(\d+)~$");

    // Regex patterns for ANSI arrow keys (Up, Down, Left, Right)
    private readonly Regex _arrowKeyPattern = new (@"^\u001b\[(1;(\d+))?([A-D])$", RegexOptions.Compiled);

    /// <summary>
    ///     Parses an ANSI escape sequence into a keyboard event. Returns null if input
    ///     is not a recognized keyboard event or its syntax is not understood.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public Key? ProcessKeyboardInput (string input)
    {
        return MapAsSs3Key(input) ??
            MapAsFunctionKey (input) ??
            MapAsArrowKey (input);
    }

    private Key? MapAsSs3Key (string input)
    {
        // Match arrow key events
        Match match = _ss3Pattern.Match (input);

        if (match.Success)
        {
            char finalLetter = match.Groups [1].Value.Single();

            var k = finalLetter switch
                   {
                       'P' => Key.F1,
                       'Q' => Key.F2,
                       'R' => Key.F3,
                       'S' => Key.F4,
                       't' => Key.F5,
                       'D' => Key.CursorLeft,
                       'C' => Key.CursorRight,
                       'A' => Key.CursorUp,
                       'B' => Key.CursorDown,

                       _ => null
                   };

            Logging.Logger.LogTrace ($"{nameof (AnsiKeyboardParser)} interpreted ss3 pattern {input} as {k}");
            return k;
        }

        return null;
    }

    private Key? MapAsArrowKey (string input)
    {
        // Match arrow key events
        Match match = _arrowKeyPattern.Match (input);

        if (match.Success)
        {
            // Group 2 captures the modifier number, if present
            string modifierGroup = match.Groups [2].Value;
            char direction = match.Groups [3].Value [0];

            Key? key = direction switch
                      {
                          'A' => Key.CursorUp,
                          'B' => Key.CursorDown,
                          'C' => Key.CursorRight,
                          'D' => Key.CursorLeft,
                          _ => null
                      };

            if (key is null)
            {
                return null;
            }

            // Examples:
            // without modifiers:
            //   \u001b\[B
            // with modifiers:
            //   \u001b\[1; 2B

            if (!string.IsNullOrWhiteSpace (modifierGroup) && int.TryParse (modifierGroup, out var modifier))
            {
                key = modifier switch
                      {
                          2 => key.WithShift,
                          3 => key.WithAlt,
                          4 => key.WithAlt.WithShift,
                          5 => key.WithCtrl,
                          6 => key.WithCtrl.WithShift,
                          7 => key.WithCtrl.WithAlt,
                          8 => key.WithCtrl.WithAlt.WithShift
                      };
            }

            Logging.Logger.LogTrace ($"{nameof (AnsiKeyboardParser)} interpreted basic cursor pattern {input} as {key}");

            return key;
        }

        // It's an unrecognized keyboard event
        return null;

    }

    private Key? MapAsFunctionKey (string input)
    {
        // Match arrow key events
        Match match = _functionKey.Match (input);

        if (match.Success)
        {
            string functionDigit = match.Groups [1].Value;

            int digit = int.Parse (functionDigit);

            var f = digit switch
                   {
                       24 => Key.F12,
                       23 => Key.F11,
                       21 => Key.F10,
                       20 => Key.F9,
                       19 => Key.F8,
                       18 => Key.F7,
                       17 => Key.F6,
                       15 => Key.F5,
                       14 => Key.F4,
                       13 => Key.F3,
                       12 => Key.F2,
                       11 => Key.F1,
                       _ => null,
                   };

            Logging.Logger.LogTrace ($"{nameof (AnsiKeyboardParser)} interpreted function key pattern {input} as {f}");

            return f;
        }

        return null;
    }

    /// <summary>
    /// Returns <see langword="true"/> if the given escape code
    /// is a keyboard escape code (e.g. cursor key)
    /// </summary>
    /// <param name="cur">escape code</param>
    /// <returns></returns>
    public bool IsKeyboard (string cur) {
        return _ss3Pattern.IsMatch(cur) || _functionKey.IsMatch (cur) || _arrowKeyPattern.IsMatch (cur);
    }
}
