using System.Collections.Concurrent;

namespace Terminal.Gui;

public class MainLoop<T> : IMainLoop<T>
{
    public ConcurrentQueue<T> InputBuffer { get; private set; } = new ();

    public IOutputBuffer OutputBuffer { get; private set; } = new OutputBuffer();

    public AnsiResponseParser<T> Parser
    {
        get;
        private set;
    }

    public IConsoleOutput Out { get;private set; }

    /// <inheritdoc />
    public void Initialize (ConcurrentQueue<T> inputBuffer, AnsiResponseParser<T> parser, IConsoleOutput consoleOutput)
    {
        InputBuffer = inputBuffer;
        Parser = parser;
        Out = consoleOutput;
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
        OutputBuffer.SetWindowSize (20, 10);

        OutputBuffer.CurrentAttribute = new Attribute (Color.White, Color.Black);
        OutputBuffer.Move (5, 3);

        // Red
        OutputBuffer.CurrentAttribute = new Attribute (new Color (255, 0, 0), Color.Black);
        OutputBuffer.AddRune ('H');

        // Orange
        OutputBuffer.CurrentAttribute = new Attribute (new Color (255, 165, 0), Color.Black);
        OutputBuffer.AddRune ('e');

        // Yellow
        OutputBuffer.CurrentAttribute = new Attribute (new Color (255, 255, 0), Color.Black);
        OutputBuffer.AddRune ('l');

        // Green
        OutputBuffer.CurrentAttribute = new Attribute (new Color (0, 255, 0), Color.Black);
        OutputBuffer.AddRune ('l');

        // Blue
        OutputBuffer.CurrentAttribute = new Attribute (new Color (100, 100, 255), Color.Black);
        OutputBuffer.AddRune ('o');

        // Indigo
        OutputBuffer.CurrentAttribute = new Attribute (new Color (75, 0, 130), Color.Black);
        OutputBuffer.AddRune (' ');

        // Violet
        OutputBuffer.CurrentAttribute = new Attribute (new Color (238, 130, 238), Color.Black);
        OutputBuffer.AddRune ('W');

        // Red
        OutputBuffer.CurrentAttribute = new Attribute (new Color (255, 0, 0), Color.Black);
        OutputBuffer.AddRune ('o');

        // Orange
        OutputBuffer.CurrentAttribute = new Attribute (new Color (255, 165, 0), Color.Black);
        OutputBuffer.AddRune ('r');

        // Yellow
        OutputBuffer.CurrentAttribute = new Attribute (new Color (255, 255, 0), Color.Black);
        OutputBuffer.AddRune ('l');

        // Green
        OutputBuffer.CurrentAttribute = new Attribute (new Color (0, 255, 0), Color.Black);
        OutputBuffer.AddRune ('d');

        // Blue
        OutputBuffer.CurrentAttribute = new Attribute (new Color (100, 100, 255), Color.Black);
        OutputBuffer.AddRune ('!');


        Out.Write (OutputBuffer);
    }
    /// <inheritdoc />
    public void Dispose ()
    { // TODO release managed resources here
    }
}