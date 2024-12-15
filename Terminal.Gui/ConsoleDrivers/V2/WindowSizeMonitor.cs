namespace Terminal.Gui;

internal class WindowSizeMonitor : IWindowSizeMonitor
{
    private readonly IConsoleOutput _consoleOut;
    private readonly IOutputBuffer _outputBuffer;
    private Size _lastSize = new Size (0,0);

    /// <summary>Invoked when the terminal's size changed. The new size of the terminal is provided.</summary>
    public event EventHandler<SizeChangedEventArgs> SizeChanging;

    public WindowSizeMonitor (IConsoleOutput consoleOut, IOutputBuffer outputBuffer)
    {
        _consoleOut = consoleOut;
        _outputBuffer = outputBuffer;
    }

    /// <inheritdoc />
    public void Poll ()
    {
        var size = _consoleOut.GetWindowSize ();

        _outputBuffer.SetWindowSize (size.Width, size.Height);

        if (size != _lastSize)
        {
            _lastSize = size;
            SizeChanging?.Invoke (this,new (size));
        }
    }
}
