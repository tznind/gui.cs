using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Terminal.Gui;

/// <summary>
/// Singleton logging instance class. Do not use console loggers
/// with this class as it will interfere with Terminal.Gui
/// screen output (i.e. use a file logger)
/// </summary>
public static class Logging
{
    /// <summary>
    /// Logger, defaults to NullLogger (i.e. no logging).  Set this to a
    /// file logger to enable logging of Terminal.Gui internals.
    /// </summary>
    public static ILogger Logger { get; set; } = NullLogger.Instance;
}
