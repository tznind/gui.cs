using static Unix.Terminal.Curses;

namespace Terminal.Gui;

/// <summary>
/// Features specific to the windows operating system
/// </summary>
internal class WindowsFeatureSet
{

    public bool ConHostLegacyMode { get; set; }


    public override string ToString () { return $"{nameof(ConHostLegacyMode)}:{ConHostLegacyMode}"; }
}
