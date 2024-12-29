#nullable enable
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Terminal.Gui;

/// <inheritdoc/>
public class MainLoop<T> : IMainLoop<T>
{
    private ITimedEvents? _timedEvents;

    /// <inheritdoc/>
    public ITimedEvents TimedEvents
    {
        get => _timedEvents ?? throw new NotInitializedException(nameof(TimedEvents));
        private set => _timedEvents = value;
    }

    // TODO: follow above pattern for others too

    /// <summary>
    /// The input events thread-safe collection. This is populated on separate
    /// thread by a <see cref="IConsoleInput{T}"/>. Is drained as part of each
    /// <see cref="Iteration"/>
    /// </summary>
    public ConcurrentQueue<T> InputBuffer { get; private set; }

    public IInputProcessor InputProcessor { get; private set; }

    public IOutputBuffer OutputBuffer { get; } = new OutputBuffer ();

    public IConsoleOutput Out { get; private set; }
    public AnsiRequestScheduler AnsiRequestScheduler { get; private set; }

    public IWindowSizeMonitor WindowSizeMonitor { get; private set; }

    /// <summary>
    ///     Determines how to get the current system type, adjust
    ///     in unit tests to simulate specific timings.
    /// </summary>
    public Func<DateTime> Now { get; set; } = () => DateTime.Now;

    private static readonly Histogram<int> totalIterationMetric = Logging.Meter.CreateHistogram<int> ("Iteration (ms)");

    private static readonly Histogram<int> iterationInvokesAndTimeouts = Logging.Meter.CreateHistogram<int> ("Invokes & Timers (ms)");

    public void Initialize (ITimedEvents timedEvents, ConcurrentQueue<T> inputBuffer, IInputProcessor inputProcessor, IConsoleOutput consoleOutput)
    {
        InputBuffer = inputBuffer;
        Out = consoleOutput;
        InputProcessor = inputProcessor;

        TimedEvents = timedEvents;
        AnsiRequestScheduler = new (InputProcessor.GetParser ());

        WindowSizeMonitor = new WindowSizeMonitor (Out, OutputBuffer);
    }

    /// <inheritdoc/>
    public void Iteration ()
    {
        DateTime dt = Now ();

        IterationImpl ();

        TimeSpan took = Now () - dt;
        TimeSpan sleepFor = TimeSpan.FromMilliseconds (50) - took;

        totalIterationMetric.Record (took.Milliseconds);

        if (sleepFor.Milliseconds > 0)
        {
            Task.Delay (sleepFor).Wait ();
        }
    }

    public void IterationImpl ()
    {
        InputProcessor.ProcessQueue ();

        if (Application.Top != null)
        {
            bool needsDrawOrLayout = AnySubviewsNeedDrawn (Application.Top);

            bool sizeChanged = WindowSizeMonitor.Poll ();

            if (needsDrawOrLayout || sizeChanged)
            {
                // TODO: Test only
                Application.LayoutAndDraw (true);

                Out.Write (OutputBuffer);
            }
        }

        var swCallbacks = Stopwatch.StartNew ();

        TimedEvents.LockAndRunTimers ();

        TimedEvents.LockAndRunIdles ();

        iterationInvokesAndTimeouts.Record (swCallbacks.Elapsed.Milliseconds);
    }

    private bool AnySubviewsNeedDrawn (View v)
    {
        if (v.NeedsDraw || v.NeedsLayout)
        {
            return true;
        }

        foreach (View subview in v.Subviews)
        {
            if (AnySubviewsNeedDrawn (subview))
            {
                return true;
            }
        }

        return false;
    }

    /// <inheritdoc/>
    public void Dispose ()
    { // TODO release managed resources here
    }
}
