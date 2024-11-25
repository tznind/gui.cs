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

        Random r = new Random ();

        // TODO: throttle this
        var size = Out.GetWindowSize ();

        OutputBuffer.SetWindowSize (size.Width, size.Height);
        // TODO: Test only

        var w = new Window
        {
            Title = "Hello World",
            Width = 30,
            Height = 5
        };
        w.Add (new Button { Text = "OMG!", X = 5, Y = 2 ,Width = Dim.Auto ()});
        w.Layout ();
        w.Draw ();


        OutputBuffer.CurrentAttribute = new Attribute (Color.White, Color.Black);
        OutputBuffer.Move (_lastMousePos.X,_lastMousePos.Y);

        foreach (var ch in sb.ToString())
        {
            OutputBuffer.CurrentAttribute = new Attribute (new Color (r.Next (255), r.Next (255), r.Next (255)), Color.Black);
            OutputBuffer.AddRune (ch);
        }

        Out.Write (OutputBuffer);
    }

    /// <inheritdoc />
    public void Dispose ()
    { // TODO release managed resources here
    }
}