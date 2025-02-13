#nullable enable
namespace Terminal.Gui;


/// <summary>
/// Handles bespoke behaviours that occur when application top level changes.
/// </summary>
public class ToplevelTransitionManager : IToplevelTransitionManager
{
    private readonly HashSet<Toplevel> _readiedTopLevels = new HashSet<Toplevel> ();

    private View? _lastTop;

    /// <inheritdoc />
    public void RaiseReadyEventIfNeeded ()
    {
        var top = Application.Top;
        if (top != null && !_readiedTopLevels.Contains (top))
        {
            top.OnReady ();
            _readiedTopLevels.Add (top);
        }
    }

    /// <inheritdoc />
    public void HandleTopMaybeChanging ()
    {
        var newTop = Application.Top;
        if (_lastTop != null && _lastTop != newTop && newTop != null)
        {
            newTop.SetNeedsDraw ();
        }

        _lastTop = Application.Top;
    }
}
