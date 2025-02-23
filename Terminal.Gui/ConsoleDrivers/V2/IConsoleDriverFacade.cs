namespace Terminal.Gui;

/// <summary>
/// Interface for v2 driver abstraction layer
/// </summary>
public interface IConsoleDriverFacade
{
    public IInputProcessor InputProcessor { get; }
}
