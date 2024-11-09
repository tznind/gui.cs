#nullable enable
namespace Terminal.Gui;

/// <summary>
///     Represents the status of an ANSI escape sequence request made to the terminal using
///     <see cref="AnsiEscapeSequenceRequests"/>.
/// </summary>
/// <remarks></remarks>
public class AnsiEscapeSequenceRequestStatus
{
    /// <summary>
    /// The time at which the request was sent
    /// </summary>
    public DateTime Sent { get; } = DateTime.Now;

    /// <summary>Creates a new state of escape sequence request.</summary>
    /// <param name="ansiRequest">The <see cref="AnsiEscapeSequenceRequest"/> object.</param>
    public AnsiEscapeSequenceRequestStatus (AnsiEscapeSequenceRequest ansiRequest) { AnsiRequest = ansiRequest; }

    /// <summary>Gets the Escape Sequence Terminator (e.g. ESC[8t ... t is the terminator).</summary>
    public AnsiEscapeSequenceRequest AnsiRequest { get; }
}
