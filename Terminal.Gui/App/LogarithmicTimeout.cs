namespace Terminal.Gui.App;

/// <summary>Implements a logarithmic increasing timeout.</summary>
public class LogarithmicTimeout : Timeout
{
    private int stage = 0;
    private readonly TimeSpan baseDelay;

    /// <summary>
    /// Creates a new instance where stages are the logarithm multiplied by the
    /// <paramref name="baseDelay"/> (starts fast then slows).
    /// </summary>
    /// <param name="baseDelay">Multiple for the logarithm</param>
    /// <param name="callback">Method to invoke</param>
    public LogarithmicTimeout (TimeSpan baseDelay, Func<bool> callback)
    {
        this.baseDelay = baseDelay;
        this.Callback = callback;
    }

    /// <summary>Gets the current calculated Span based on the stage.</summary>
    public override TimeSpan Span
    {
        get
        {
            // Calculate logarithmic increase
            double multiplier = Math.Log (stage + 1); // ln(stage + 1)
            return TimeSpan.FromMilliseconds (baseDelay.TotalMilliseconds * multiplier);
        }
    }

    /// <summary>Increments the stage to increase the timeout.</summary>
    public void AdvanceStage ()
    {
        stage++;
    }

    /// <summary>Resets the stage back to zero.</summary>
    public void Reset ()
    {
        stage = 0;
    }
}