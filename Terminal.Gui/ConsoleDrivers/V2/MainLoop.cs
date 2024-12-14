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


        if (Application.Top != null)
        {

            bool needsDrawOrLayout = AnySubviewsNeedDrawn(Application.Top);

            if (needsDrawOrLayout)
            {
                // TODO: throttle this
                var size = Out.GetWindowSize ();

                OutputBuffer.SetWindowSize (size.Width, size.Height);

                // TODO: Test only

                Application.Top.Layout ();
                Application.Top.Draw ();

                Out.Write (OutputBuffer);
            }
        }

        TimedEvents.LockAndRunTimers ();

        TimedEvents.LockAndRunIdles ();
    }

    private bool AnySubviewsNeedDrawn (View v)
    {
        if (v.NeedsDraw || v.NeedsLayout)
        {
            return true;
        }

        foreach(var subview in v.Subviews )
        {
            if (AnySubviewsNeedDrawn (subview))
            {
                return true;
            }
        }

        return false;
    }

    /// <inheritdoc />
    public void Dispose ()
    { // TODO release managed resources here
    }
}