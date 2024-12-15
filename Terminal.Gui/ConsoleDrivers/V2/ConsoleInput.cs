#nullable enable
using System.Collections.Concurrent;

namespace Terminal.Gui;

public abstract class ConsoleInput<T> : IConsoleInput<T>
{
    private ConcurrentQueue<T>? _inputBuffer;

    /// <summary>
    /// Determines how to get the current system type, adjust
    /// in unit tests to simulate specific timings.
    /// </summary>
    public Func<DateTime> Now { get; set; } = ()=>DateTime.Now;

    /// <inheritdoc />
    public virtual void Dispose ()
    {

    }

    /// <inheritdoc />
    public void Initialize (ConcurrentQueue<T> inputBuffer)
    {
        _inputBuffer = inputBuffer;
    }

    /// <inheritdoc />
    public void Run (CancellationToken token)
    {
        if (_inputBuffer == null)
        {
            throw new ("Cannot run input before Initialization");
        }

        do
        {
            var dt = Now ();

            if (Peek ())
            {
                foreach (var r in Read ())
                {
                    _inputBuffer.Enqueue (r);
                }
            }

            var took = Now () - dt;
            var sleepFor = TimeSpan.FromMilliseconds (20) - took;

            if (sleepFor.Milliseconds > 0)
            {
                Task.Delay (sleepFor, token).Wait (token);
            }

            token.ThrowIfCancellationRequested ();
        }
        while (!token.IsCancellationRequested);
    }

    /// <summary>
    /// When implemented in a derived class, returns true if there is data available
    /// to read from console.
    /// </summary>
    /// <returns></returns>
    protected abstract bool Peek ();

    /// <summary>
    /// Returns the available data without blocking, called when <see cref="Peek"/>
    /// returns <see langword="true"/>.
    /// </summary>
    /// <returns></returns>
    protected abstract IEnumerable<T> Read ();
}
