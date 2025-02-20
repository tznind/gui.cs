#nullable enable
namespace Terminal.Gui;

/// <summary>
/// Base class for ANSI keyboard parsing patterns.
/// </summary>
public abstract class AnsiKeyboardParserPattern
{
    public abstract bool IsMatch (string input);
    public abstract Key? GetKey (string input);
}
