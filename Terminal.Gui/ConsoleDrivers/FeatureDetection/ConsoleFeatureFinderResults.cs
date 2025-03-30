namespace Terminal.Gui;

/// <summary>
/// Results of console feature detection
/// </summary>
internal class ConsoleFeatureFinderResults
{
    public WindowsFeatureSet Windows { get; set; } = new WindowsFeatureSet();
    public bool IsWindows { get; set; }

    /// <inheritdoc />
    public override string ToString ()
    {
        return $"{nameof(IsWindows)}:{IsWindows} {nameof(Windows)}:{Windows}";
    }
}