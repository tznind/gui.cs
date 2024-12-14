using System.Collections.Concurrent;

namespace Terminal.Gui;

public class MainLoop<T> : IMainLoop<T>
{
    public ITimedEvents TimedEvents { get; private set; }
    public ConcurrentQueue<T> InputBuffer { get; private set; } = new ();

    public IInputProcessor InputProcessor { get; private set; }

    public IOutputBuffer OutputBuffer { get; private set; } = new OutputBuffer();

    public IConsoleOutput Out { get;private set; }
    public AnsiRequestScheduler AnsiRequestScheduler { get; private set; }

    public void Initialize (ITimedEvents timedEvents, ConcurrentQueue<T> inputBuffer, IInputProcessor inputProcessor, IConsoleOutput consoleOutput)
    {
        InputBuffer = inputBuffer;
        Out = consoleOutput;
        InputProcessor = inputProcessor;

        TimedEvents = timedEvents;
        AnsiRequestScheduler = new AnsiRequestScheduler (InputProcessor.GetParser ());

    }

    public void Run (CancellationToken token)
    {
        do
        {
            var dt = DateTime.Now;

            Iteration ();

            var took = DateTime.Now - dt;
            var sleepFor = TimeSpan.FromMilliseconds (50) - took;

            if (sleepFor.Milliseconds > 0)
            {
                Task.Delay (sleepFor, token).Wait (token);
            }
        }
        while (!token.IsCancellationRequested);
    }

    private bool first = true;
    /// <inheritdoc />
    public void Iteration ()
    {
        InputProcessor.ProcessQueue ();

 
            // TODO: throttle this
            var size = Out.GetWindowSize ();

            OutputBuffer.SetWindowSize (size.Width, size.Height);
            // TODO: Test only


        if (Application.Top != null)
        {
            Application.LayoutAndDraw ();
            Out.Write (OutputBuffer);
        }


        TimedEvents.LockAndRunTimers ();

        TimedEvents.LockAndRunIdles ();
    }

    /// <inheritdoc />
    public void Dispose ()
    { // TODO release managed resources here
    }
}