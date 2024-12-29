using System.Diagnostics.Metrics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Terminal.Gui;

/// <summary>
///     Singleton logging instance class. Do not use console loggers
///     with this class as it will interfere with Terminal.Gui
///     screen output (i.e. use a file logger).
/// </summary>
/// <remarks>
///     Also contains the
///     <see cref="Meter"/> instance that should be used for internal metrics
///     (iteration timing etc).
/// </remarks>
public static class Logging
{
    /// <summary>
    ///     Logger, defaults to NullLogger (i.e. no logging).  Set this to a
    ///     file logger to enable logging of Terminal.Gui internals.
    /// </summary>
    public static ILogger Logger { get; set; } = NullLogger.Instance;

    /// <summary>
    ///     Metrics reporting meter for internal Terminal.Gui processes. To use
    ///     create your own static instrument e.g. CreateCounter, CreateHistogram etc
    /// </summary>
    internal static readonly Meter Meter = new ("Terminal.Gui");

    /// <summary>
    /// Metric for how long it takes each full iteration of the main loop to occur
    /// </summary>
    public static readonly Histogram<int> TotalIterationMetric = Logging.Meter.CreateHistogram<int> ("Iteration (ms)");

    /// <summary>
    /// Metric for how long it took to do the 'timeouts and invokes' section of main loop.
    /// </summary>
    public static readonly Histogram<int> IterationInvokesAndTimeouts = Logging.Meter.CreateHistogram<int> ("Invokes & Timers (ms)");

    /// <summary>
    /// Metric for how long it takes to read all available input from the input stream - at which
    /// point input loop will sleep.
    /// </summary>
    public static readonly Histogram<int> DrainInputStream = Logging.Meter.CreateHistogram<int> ("Drain Input (ms)");

}
