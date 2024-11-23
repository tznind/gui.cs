namespace Terminal.Gui;
public interface IConsoleOutput : IDisposable
{
    void Write(string text);
    void Write (IOutputBuffer buffer);
    public Size GetWindowSize ();
}
