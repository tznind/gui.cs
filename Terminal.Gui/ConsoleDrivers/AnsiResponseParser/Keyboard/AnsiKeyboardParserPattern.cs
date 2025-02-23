#nullable enable
using Microsoft.Extensions.Logging;

namespace Terminal.Gui;

/// <summary>
/// Base class for ANSI keyboard parsing patterns.
/// </summary>
public abstract class AnsiKeyboardParserPattern
{
    /// <summary>
    /// Does this pattern dangerously overlap with other sequences
    /// such that it should only be applied at the lsat second after
    /// all other sequences have been tried.
    /// <remarks>
    /// When <see langword="true"/> this pattern will only be used
    /// at <see cref="AnsiResponseParser.Release"/> time.
    /// </remarks>
    /// </summary>
    public bool IsLastMinute { get; set; }

    public abstract bool IsMatch (string input);
    private string _name;

    public AnsiKeyboardParserPattern ()
    {
        _name = GetType ().Name;
    }
    public Key? GetKey (string input)
    {
        var key = GetKeyImpl (input);
        Logging.Logger.LogTrace ($"{nameof (AnsiKeyboardParser)} interpreted {input} as {key} using {_name}");

        return key;
    }

    protected abstract Key? GetKeyImpl (string input);
}
