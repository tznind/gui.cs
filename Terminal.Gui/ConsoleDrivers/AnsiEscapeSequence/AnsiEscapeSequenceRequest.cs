#nullable enable
namespace Terminal.Gui;

/// <summary>
///     Describes an ongoing ANSI request sent to the console.
///     Use <see cref="ResponseReceived"/> to handle the response
///     when console answers the request.
/// </summary>
public class AnsiEscapeSequenceRequest
{
    internal readonly object _responseLock = new (); // Per-instance lock

    /// <summary>
    ///     Request to send e.g. see
    ///     <see>
    ///         <cref>EscSeqUtils.CSI_SendDeviceAttributes.Request</cref>
    ///     </see>
    /// </summary>
    public required string Request { get; init; }

    /// <summary>
    ///     Response received from the request.
    /// </summary>
    public string Response { get; internal set; } = string.Empty;

    /// <summary>
    ///     Invoked when the console responds with an ANSI response code that matches the
    ///     <see cref="Terminator"/>
    /// </summary>
    public event EventHandler<AnsiEscapeSequenceResponse>? ResponseReceived;

    /// <summary>
    ///     <para>
    ///         The terminator that uniquely identifies the type of response as responded
    ///         by the console. e.g. for
    ///         <see>
    ///             <cref>EscSeqUtils.CSI_SendDeviceAttributes.Request</cref>
    ///         </see>
    ///         the terminator is
    ///         <see>
    ///             <cref>EscSeqUtils.CSI_SendDeviceAttributes.Terminator</cref>
    ///         </see>
    ///         .
    ///     </para>
    ///     <para>
    ///         After sending a request, the first response with matching terminator will be matched
    ///         to the oldest outstanding request.
    ///     </para>
    /// </summary>
    public required string Terminator { get; init; }

    /// <summary>
    ///     Execute an ANSI escape sequence escape which may return a response or error.
    /// </summary>
    /// <param name="ansiRequest">The ANSI escape sequence to request.</param>
    /// <param name="result">
    ///     When this method returns <see langword="true"/>, an object containing the response with an empty
    ///     error.
    /// </param>
    /// <returns>A <see cref="AnsiEscapeSequenceResponse"/> with the response, error, terminator and value.</returns>
    public static bool TryExecuteAnsiRequest (AnsiEscapeSequenceRequest ansiRequest, out AnsiEscapeSequenceResponse result)
    {
        var error = new StringBuilder ();
        var values = new string? [] { null };

        try
        {
            ConsoleDriver? driver = Application.Driver;

            // Send the ANSI escape sequence
            ansiRequest.Response = driver?.WriteAnsiRequest (ansiRequest)!;

            if (!string.IsNullOrEmpty (ansiRequest.Response) && !ansiRequest.Response.StartsWith (EscSeqUtils.KeyEsc))
            {
                throw new InvalidOperationException ("Invalid escape character!");
            }

            if (string.IsNullOrEmpty (ansiRequest.Terminator))
            {
                throw new InvalidOperationException ("Terminator request is empty.");
            }

            if (!ansiRequest.Response.EndsWith (ansiRequest.Terminator [^1]))
            {
                char resp = string.IsNullOrEmpty (ansiRequest.Response) ? ' ' : ansiRequest.Response.Last ();

                throw new InvalidOperationException ($"Terminator ends with '{resp}'\nand doesn't end with: '{ansiRequest.Terminator [^1]}'");
            }
        }
        catch (Exception ex)
        {
            error.AppendLine ($"Error executing ANSI request:\n{ex.Message}");
        }
        finally
        {
            if (string.IsNullOrEmpty (error.ToString ()))
            {
                (string? _, string? _, values, string? _) = EscSeqUtils.GetEscapeResult (ansiRequest.Response.ToCharArray ());
            }
        }

        AnsiEscapeSequenceResponse ansiResponse = new ()
        {
            Response = ansiRequest.Response, Error = error.ToString (),
            Terminator = string.IsNullOrEmpty (ansiRequest.Response) ? "" : ansiRequest.Response [^1].ToString (), Value = values [0]
        };

        // Invoke the event if it's subscribed
        ansiRequest.ResponseReceived?.Invoke (ansiRequest, ansiResponse);

        result = ansiResponse;

        return string.IsNullOrWhiteSpace (result.Error) && !string.IsNullOrWhiteSpace (result.Response);
    }

    /// <summary>
    ///     The value expected in the response e.g.
    ///     <see>
    ///         <cref>EscSeqUtils.CSI_ReportTerminalSizeInChars.Value</cref>
    ///     </see>
    ///     which will have a 't' as terminator but also other different request may return the same terminator with a
    ///     different value.
    /// </summary>
    public string? Value { get; init; }

    internal void RaiseResponseFromInput (AnsiEscapeSequenceRequest ansiRequest, string response) { ResponseFromInput?.Invoke (ansiRequest, response); }

    internal event EventHandler<string>? ResponseFromInput;

    /// <summary>
    /// Raises the <see cref="ResponseReceived"/> event
    /// </summary>
    /// <param name="response"></param>
    internal void OnResponseReceived (string response)
    {
        ResponseReceived?.Invoke (this,
                                  new()
                                  {
                                      Error = string.Empty,
                                      Response = response,
                                      Terminator = Terminator,
                                      Value = null
                                  });

    }
    /// <summary>
    /// Raises the <see cref="ResponseReceived"/> event
    /// with <see cref="AnsiEscapeSequenceResponse.Error"/>
    /// </summary>
    internal void OnAbandoned ()
    {
        ResponseReceived?.Invoke (this,
                                  new ()
                                  {
                                      Error = "No response from terminal",
                                      Response = string.Empty,
                                      Terminator = Terminator,
                                      Value = null
                                  });

    }


    /// <summary>
    /// Sends the <see cref="Request"/> direct to the console out (requires an <see cref="AnsiResponseParser"/>
    /// to be in place in application driver.
    /// </summary>
    public void Send ()
    {
        Application.Driver?.WriteRaw (Request);
    }
}
