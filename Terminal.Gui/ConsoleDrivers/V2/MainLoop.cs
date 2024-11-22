using System.Collections.Concurrent;
using Terminal.Gui.ConsoleDrivers;
using Terminal.Gui.ConsoleDrivers.V2;
using static Terminal.Gui.WindowsConsole;

namespace Terminal.Gui;

public class MainLoop<T> : IMainLoop<T>
{
    public ConcurrentQueue<T> InputBuffer { get; private set; } = new ();

    public IInputProcessor InputProcessor { get; private set; }

    public IOutputBuffer OutputBuffer { get; private set; } = new OutputBuffer();

    public IConsoleOutput Out { get;private set; }


    // TODO: Remove later
    StringBuilder sb = new StringBuilder ();

    public void Initialize (ConcurrentQueue<T> inputBuffer, IConsoleOutput consoleOutput)
    {
        InputBuffer = inputBuffer;
        Out = consoleOutput;
        InputProcessor = new InputProcessor<T> (inputBuffer);

        // TODO: Remove later
        InputProcessor.KeyDown += (s,k) => sb.Append (ConsoleKeyMapping.ToChar (k));
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
        OutputBuffer.SetWindowSize (20, 10);

        OutputBuffer.CurrentAttribute = new Attribute (Color.White, Color.Black);
        OutputBuffer.Move (0,0);

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