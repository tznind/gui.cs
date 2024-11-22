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
        OutputBuffer.AddStr ("Hello World!");
        Out.Write (OutputBuffer);
    }
    /// <inheritdoc />
    public void Dispose ()
    { // TODO release managed resources here
    }
}