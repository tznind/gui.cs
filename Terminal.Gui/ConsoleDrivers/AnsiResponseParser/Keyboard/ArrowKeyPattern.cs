#nullable enable
using System.Text.RegularExpressions;

namespace Terminal.Gui;

public class ArrowKeyPattern : AnsiKeyboardParserPattern
{
    private Dictionary<char, Key> _terminators = new Dictionary<char, Key> ()
    {
        { 'A', Key.CursorUp },
        {'B',Key.CursorDown},
        {'C',Key.CursorRight},
        {'D',Key.CursorLeft}
    };

    private readonly Regex _pattern;

    public override bool IsMatch (string input) => _pattern.IsMatch (input);

    public ArrowKeyPattern ()
    {
        var terms = new string (_terminators.Select (k => k.Key).ToArray ());
        _pattern = new (@$"^\u001b\[(1;(\d+))?([{terms}])$");
    }
    protected override Key? GetKeyImpl (string input)
    {
        var match = _pattern.Match (input);

        if (!match.Success)
        {
            return null;
        }

        char terminator = match.Groups [3].Value [0];
        string modifierGroup = match.Groups [2].Value;

        var key = _terminators.GetValueOrDefault (terminator);

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
