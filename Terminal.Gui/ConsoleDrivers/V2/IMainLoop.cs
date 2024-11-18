using System.Collections.Concurrent;

namespace Terminal.Gui;

internal interface IMainLoop<T> : IDisposable
{
    /// <summary>
    /// Initializes the loop with a buffer from which data can be read
    /// </summary>
    /// <param name="inputBuffer"></param>
    /// <param name="parser"></param>
    void Initialize (ConcurrentQueue<T> inputBuffer, AnsiResponseParser<T> parser);

    /// <summary>
    /// Runs <see cref="Iteration"/> in an infinite loop.
    /// </summary>
    /// <param name="token"></param>
    /// <exception cref="OperationCanceledException">Raised when token is
    /// cancelled. This is the only means of exiting.</exception>
    public void Run (CancellationToken token);

    /// <summary>
    /// Perform a single iteration of the main loop without blocking anywhere.
    /// </summary>
    public void Iteration ();
}
