using System.Collections.Concurrent;

namespace Terminal.Gui;

abstract class ConsoleInput<T>(ConcurrentQueue<T> inputBuffer) : IConsoleInput
{
    /// <inheritdoc />
    public virtual void Dispose ()
    {

    }

    /// <inheritdoc />
    public void Run (CancellationToken token)
    {
        do
        {
            if (Peek ())
            {
                foreach (var r in Read ())
                {
                    inputBuffer.Enqueue (r);
                }
            }

            Task.Delay (TimeSpan.FromMilliseconds (20), token).Wait (token);
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
