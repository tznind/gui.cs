﻿#nullable enable

namespace Terminal.Gui;

internal class AnsiResponseParser<T>
{
    private readonly List<Tuple<char,T>> held = new ();
    private readonly List<(string terminator, Action<string> response)> expectedResponses = new ();

    // Enum to manage the parser's state
    private enum ParserState
    {
        Normal,
        ExpectingBracket,
        InResponse
    }

    // Current state of the parser
    private ParserState currentState = ParserState.Normal;
    private readonly HashSet<char> _knownTerminators = new ();

    /*
     * ANSI Input Sequences
     *
     * \x1B[A   // Up Arrow key pressed
     * \x1B[B   // Down Arrow key pressed
     * \x1B[C   // Right Arrow key pressed
     * \x1B[D   // Left Arrow key pressed
     * \x1B[3~  // Delete key pressed
     * \x1B[2~  // Insert key pressed
     * \x1B[5~  // Page Up key pressed
     * \x1B[6~  // Page Down key pressed
     * \x1B[1;5D // Ctrl + Left Arrow
     * \x1B[1;5C // Ctrl + Right Arrow
     * \x1B[0;10;20M // Mouse button pressed at position (10, 20)
     * \x1B[0c  // Device Attributes Response (e.g., terminal identification)
     */

    public AnsiResponseParser ()
    {
        // These all are valid terminators on ansi responses,
        // see CSI in https://invisible-island.net/xterm/ctlseqs/ctlseqs.html#h3-Functions-using-CSI-_-ordered-by-the-final-character_s
        _knownTerminators.Add ('@');
        _knownTerminators.Add ('A');
        _knownTerminators.Add ('B');
        _knownTerminators.Add ('C');
        _knownTerminators.Add ('D');
        _knownTerminators.Add ('E');
        _knownTerminators.Add ('F');
        _knownTerminators.Add ('G');
        _knownTerminators.Add ('G');
        _knownTerminators.Add ('H');
        _knownTerminators.Add ('I');
        _knownTerminators.Add ('J');
        _knownTerminators.Add ('K');
        _knownTerminators.Add ('L');
        _knownTerminators.Add ('M');

        // No - N or O
        _knownTerminators.Add ('P');
        _knownTerminators.Add ('Q');
        _knownTerminators.Add ('R');
        _knownTerminators.Add ('S');
        _knownTerminators.Add ('T');
        _knownTerminators.Add ('W');
        _knownTerminators.Add ('X');
        _knownTerminators.Add ('Z');

        _knownTerminators.Add ('^');
        _knownTerminators.Add ('`');
        _knownTerminators.Add ('~');

        _knownTerminators.Add ('a');
        _knownTerminators.Add ('b');
        _knownTerminators.Add ('c');
        _knownTerminators.Add ('d');
        _knownTerminators.Add ('e');
        _knownTerminators.Add ('f');
        _knownTerminators.Add ('g');
        _knownTerminators.Add ('h');
        _knownTerminators.Add ('i');

        _knownTerminators.Add ('l');
        _knownTerminators.Add ('m');
        _knownTerminators.Add ('n');

        _knownTerminators.Add ('p');
        _knownTerminators.Add ('q');
        _knownTerminators.Add ('r');
        _knownTerminators.Add ('s');
        _knownTerminators.Add ('t');
        _knownTerminators.Add ('u');
        _knownTerminators.Add ('v');
        _knownTerminators.Add ('w');
        _knownTerminators.Add ('x');
        _knownTerminators.Add ('y');
        _knownTerminators.Add ('z');

        // Add more if necessary
    }

    /// <summary>
    ///     Processes input which may be a single character or multiple.
    ///     Returns what should be passed on to any downstream input processing
    ///     (i.e., removes expected ANSI responses from the input stream).
    /// </summary>
    public IEnumerable<Tuple<char,T>> ProcessInput (params Tuple<char,T>[] input)
    {
        var output = new List<Tuple<char, T>> (); // Holds characters that should pass through
        var index = 0; // Tracks position in the input string

        while (index < input.Length)
        {
            var currentChar = input [index];

            switch (currentState)
            {
                case ParserState.Normal:
                    if (currentChar.Item1 == '\x1B')
                    {
                        // Escape character detected, move to ExpectingBracket state
                        currentState = ParserState.ExpectingBracket;
                        held.Add (currentChar); // Hold the escape character
                        index++;
                    }
                    else
                    {
                        // Normal character, append to output
                        output.Add (currentChar);
                        index++;
                    }

                    break;

                case ParserState.ExpectingBracket:
                    if (currentChar.Item1 == '[')
                    {
                        // Detected '[' , transition to InResponse state
                        currentState = ParserState.InResponse;
                        held.Add (currentChar); // Hold the '['
                        index++;
                    }
                    else
                    {
                        // Invalid sequence, release held characters and reset to Normal
                        output.AddRange (held);
                        output.Add (currentChar); // Add current character
                        ResetState ();
                        index++;
                    }

                    break;

                case ParserState.InResponse:
                    held.Add (currentChar);

                    // Check if the held content should be released
                    var handled = HandleHeldContent ();

                    if (handled != null)
                    {
                        output.AddRange (handled);
                        ResetState (); // Exit response mode and reset
                    }

                    index++;

                    break;
            }
        }

        return output; // Return all characters that passed through
    }

    /// <summary>
    ///     Resets the parser's state when a response is handled or finished.
    /// </summary>
    private void ResetState ()
    {
        currentState = ParserState.Normal;
        held.Clear ();
    }

    /// <summary>
    ///     Checks the current `held` content to decide whether it should be released, either as an expected or unexpected
    ///     response.
    /// </summary>
    private IEnumerable<Tuple<char,T>>? HandleHeldContent ()
    {
        string cur = HeldToString ();

        // Check for expected responses
        (string terminator, Action<string> response) matchingResponse = expectedResponses.FirstOrDefault (r => cur.EndsWith (r.terminator));

        if (matchingResponse.response != null)
        {
            DispatchResponse (matchingResponse.response);
            expectedResponses.Remove (matchingResponse);

            return null;
        }

        if (_knownTerminators.Contains (cur.Last ()) && cur.StartsWith (EscSeqUtils.CSI))
        {
            // Detected a response that we were not expecting
            return held;
        }

        // Add more cases here for other standard sequences (like arrow keys, function keys, etc.)

        // If no match, continue accumulating characters
        return null;
    }

    private string HeldToString ()
    {
        return new string (held.Select (h => h.Item1).ToArray ());
    }

    private void DispatchResponse (Action<string> response)
    {
        // If it matches the expected response, invoke the callback and return nothing for output
        response?.Invoke (HeldToString ());
        ResetState ();
    }

    /// <summary>
    ///     Registers a new expected ANSI response with a specific terminator and a callback for when the response is
    ///     completed.
    /// </summary>
    public void ExpectResponse (string terminator, Action<string> response) { expectedResponses.Add ((terminator, response)); }
}
