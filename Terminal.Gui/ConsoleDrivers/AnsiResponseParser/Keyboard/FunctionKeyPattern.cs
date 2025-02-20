#nullable enable
using System.Text.RegularExpressions;

namespace Terminal.Gui;

public class FunctionKeyPattern : AnsiKeyboardParserPattern
{
    private static readonly Regex _pattern = new (@"^\u001b\[(\d+)~$");

    public override bool IsMatch (string input) => _pattern.IsMatch (input);

    public override Key? GetKey (string input)
    {
        var match = _pattern.Match (input);

        if (!match.Success)
        {
            return null;
        }

        return int.Parse (match.Groups [1].Value) switch
               {
                   11 => Key.F1,
                   12 => Key.F2,
                   13 => Key.F3,
                   14 => Key.F4,
                   15 => Key.F5,
                   17 => Key.F6,
                   18 => Key.F7,
                   19 => Key.F8,
                   20 => Key.F9,
                   21 => Key.F10,
                   23 => Key.F11,
                   24 => Key.F12,
                   _ => null
               };
    }
}
