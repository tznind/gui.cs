using System.Collections.Concurrent;

namespace Terminal.Gui;

internal interface IConsoleInput : IDisposable
{
    /// <summary>
    /// Runs in an infinite input loop.
    /// </summary>
    /// <param name="token"></param>
    /// <exception cref="OperationCanceledException">Raised when token is
    /// cancelled. This is the only means of exiting the input.</exception>
    void Run (CancellationToken token);

}
