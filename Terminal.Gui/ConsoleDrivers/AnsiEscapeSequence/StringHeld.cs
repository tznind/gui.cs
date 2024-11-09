#nullable enable
namespace Terminal.Gui;

/// <summary>
///     Implementation of <see cref="IHeld"/> for <see cref="AnsiResponseParser"/>
/// </summary>
internal class StringHeld : IHeld
{
    private readonly StringBuilder held = new ();

    public void ClearHeld () { held.Clear (); }

    public string HeldToString () { return held.ToString (); }

    public IEnumerable<object> HeldToObjects () { return held.ToString ().Select (c => (object)c); }

    public void AddToHeld (object o) { held.Append ((char)o); }
}
