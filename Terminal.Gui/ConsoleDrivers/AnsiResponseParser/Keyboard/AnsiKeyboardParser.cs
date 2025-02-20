#nullable enable
using Microsoft.Extensions.Logging;

namespace Terminal.Gui;

/// <summary>
/// Parses ANSI escape sequence strings that describe keyboard activity into <see cref="Key"/>.
/// </summary>
public class AnsiKeyboardParser
{
    private readonly List<AnsiKeyboardParserPattern> _patterns = new ()
    {
        new Ss3Pattern(),
        new FunctionKeyPattern(),
        new ArrowKeyPattern()
    };

    public Key? ProcessKeyboardInput (string input)
    {
        foreach (var pattern in _patterns)
        {
            if (pattern.IsMatch (input))
            {
                var key = pattern.GetKey (input);
                if (key != null)
                {
                    Logging.Logger.LogTrace ($"{nameof (AnsiKeyboardParser)} interpreted {input} as {key}");
                    return key;
                }
            }
        }
        return null;
    }

    public bool IsKeyboard (string input)
    {
        return _patterns.Any (pattern => pattern.IsMatch (input));
    }
}
