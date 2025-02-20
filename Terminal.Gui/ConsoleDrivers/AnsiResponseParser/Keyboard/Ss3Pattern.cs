#nullable enable
using System.Text.RegularExpressions;

namespace Terminal.Gui;

public class Ss3Pattern : AnsiKeyboardParserPattern
{
    private static readonly Regex _pattern = new (@"^\u001bO([PQRStDCAB])$");

    public override bool IsMatch (string input) => _pattern.IsMatch (input);

    public override Key? GetKey (string input)
    {
        var match = _pattern.Match (input);

        if (!match.Success)
        {
            return null;
        }

        return match.Groups [1].Value.Single () switch
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
    }
}
