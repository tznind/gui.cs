#nullable enable
namespace Terminal.Gui;

public interface IWindowSizeMonitor
{
    /// <summary>Invoked when the terminal's size changed. The new size of the terminal is provided.</summary>
    event EventHandler<SizeChangedEventArgs>? SizeChanging;

    bool Poll ();
}
