#nullable enable
using System.Collections.Concurrent;
using static Unix.Terminal.Curses;

namespace Terminal.Gui;

public interface IMainLoop<T> : IDisposable
{

    public ITimedEvents TimedEvents { get; }
    public IOutputBuffer OutputBuffer { get; }
    public IInputProcessor InputProcessor { get; }

    public AnsiRequestScheduler AnsiRequestScheduler { get; }

    public IWindowSizeMonitor WindowSizeMonitor { get; }

    /// <summary>
    /// Initializes the loop with a buffer from which data can be read
    /// </summary>
    /// <param name="timedEvents"></param>
    /// <param name="inputBuffer"></param>
    /// <param name="inputProcessor"></param>
    /// <param name="consoleOutput"></param>
    void Initialize (ITimedEvents timedEvents, ConcurrentQueue<T> inputBuffer, IInputProcessor inputProcessor, IConsoleOutput consoleOutput);

    /// <summary>
    /// Perform a single iteration of the main loop without blocking anywhere.
    /// </summary>
    public void Iteration ();
}

public interface IWindowSizeMonitor
{
    /// <summary>Invoked when the terminal's size changed. The new size of the terminal is provided.</summary>
    event EventHandler<SizeChangedEventArgs>? SizeChanging;

    bool Poll ();
}
