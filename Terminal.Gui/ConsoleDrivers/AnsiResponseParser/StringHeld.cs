﻿#nullable enable
namespace Terminal.Gui;

/// <summary>
///     Implementation of <see cref="IHeld"/> for <see cref="AnsiResponseParser"/>
/// </summary>
internal class StringHeld : IHeld
{
    private readonly StringBuilder _held = new ();

    public void ClearHeld () { _held.Clear (); }

    public string? HeldToString () { return _held.ToString (); }

    public IEnumerable<object> HeldToObjects () { return _held.ToString ().Select (c => (object)c); }

    public void AddToHeld (object o) { _held.Append ((char)o); }

    /// <inheritdoc/>
    public int Length => _held.Length;
}
