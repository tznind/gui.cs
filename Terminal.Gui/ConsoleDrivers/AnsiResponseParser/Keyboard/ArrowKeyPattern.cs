#nullable enable
using System.Text.RegularExpressions;

namespace Terminal.Gui;

public class ArrowKeyPattern : AnsiKeyboardParserPattern
{
    private static readonly Regex _pattern = new (@"^\u001b\[(1;(\d+))?([A-D])$");

    public override bool IsMatch (string input) => _pattern.IsMatch (input);

    public override Key? GetKey (string input)
    {
        var match = _pattern.Match (input);

        if (!match.Success)
        {
            return null;
        }

        char direction = match.Groups [3].Value [0];
        string modifierGroup = match.Groups [2].Value;

        Key? key = direction switch
                   {
                       'A' => Key.CursorUp,
                       'B' => Key.CursorDown,
                       'C' => Key.CursorRight,
                       'D' => Key.CursorLeft,
                       _ => null
                   };

        if (key != null && int.TryParse (modifierGroup, out var modifier))
        {
            key = modifier switch
                  {
                      2 => key.WithShift,
                      3 => key.WithAlt,
                      4 => key.WithAlt.WithShift,
                      5 => key.WithCtrl,
                      6 => key.WithCtrl.WithShift,
                      7 => key.WithCtrl.WithAlt,
                      8 => key.WithCtrl.WithAlt.WithShift,
                      _ => key
                  };
        }
        return key;
    }
}
