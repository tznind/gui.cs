using Microsoft.Extensions.Logging;

namespace Terminal.Gui.Drivers;

internal class FakeWindowSizeMonitor (IConsoleOutput consoleOut, IOutputBuffer outputBuffer) : IWindowSizeMonitor
{
    private Size _lastSize = new (0, 0);

    /// <summary>Invoked when the terminal's size changed. The new size of the terminal is provided.</summary>
    public event EventHandler<SizeChangedEventArgs> SizeChanging;

    /// <inheritdoc/>
    public bool Poll ()
    {
        if (ConsoleDriver.RunningUnitTests)
        {
            return false;
        }

        Size size = consoleOut.GetWindowSize ();

        if (size != _lastSize)
        {
            Logging.Logger.LogInformation ($"Console size changes from '{_lastSize}' to {size}");
            outputBuffer.SetWindowSize (size.Width, size.Height);
            _lastSize = size;
            SizeChanging?.Invoke (this, new (size));

            return true;
        }

        return false;
    }
}
