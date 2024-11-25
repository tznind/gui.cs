using System.Collections.Concurrent;

namespace Terminal.Gui;

public class MainLoop<T> : IMainLoop<T>
{
    public ConcurrentQueue<T> InputBuffer { get; private set; } = new ();

    public IInputProcessor InputProcessor { get; private set; }

    public IOutputBuffer OutputBuffer { get; private set; } = new OutputBuffer();

    public IConsoleOutput Out { get;private set; }
    public AnsiRequestScheduler AnsiRequestScheduler { get; private set; }


    // TODO: Remove later
    StringBuilder sb = new StringBuilder ("*");
    private Point _lastMousePos = new Point(0,0);

    public void Initialize (ConcurrentQueue<T> inputBuffer, IInputProcessor inputProcessor, IConsoleOutput consoleOutput)
    {
        InputBuffer = inputBuffer;
        Out = consoleOutput;
        InputProcessor = inputProcessor;

        AnsiRequestScheduler = new AnsiRequestScheduler (InputProcessor.GetParser ());

        // TODO: Remove later
        InputProcessor.KeyDown += (s,k) => sb.Append ((char)k);
        InputProcessor.MouseEvent += (s, e) => { _lastMousePos = e.Position; };
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
            Application.Top.NeedsDraw = true;
            Application.Top.Layout ();
            Application.Top.Draw ();
        }

        Out.Write (OutputBuffer);
    }

    /// <inheritdoc />
    public void Dispose ()
    { // TODO release managed resources here
    }
}