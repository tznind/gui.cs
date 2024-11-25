namespace Terminal.Gui;
public interface IConsoleOutput : IDisposable
{
    void Write(string text);
    void Write (IOutputBuffer buffer);
    public Size GetWindowSize ();
    void SetCursorVisibility (CursorVisibility visibility);
    void SetCursorPosition (int col, int row);
}
