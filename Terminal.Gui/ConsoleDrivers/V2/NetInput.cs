namespace Terminal.Gui;

public class NetInput : ConsoleInput<ConsoleKeyInfo>
{
    private NetWinVTConsole _adjustConsole;

    public NetInput ()
    {
        var p = Environment.OSVersion.Platform;
        if (p == PlatformID.Win32NT || p == PlatformID.Win32S || p == PlatformID.Win32Windows)
        {
            try
            {
                _adjustConsole = new NetWinVTConsole ();
            }
            catch (ApplicationException)
            {
                // Likely running as a unit test, or in a non-interactive session.
            }
        }

        // Doesn't seem to work
        Console.Out.Write (EscSeqUtils.CSI_EnableMouseEvents);
    }
    /// <inheritdoc />
    protected override bool Peek () => Console.KeyAvailable;

    /// <inheritdoc />
    protected override IEnumerable<ConsoleKeyInfo> Read ()
    {
        while (Console.KeyAvailable)
        {
            yield return Console.ReadKey (true);
        }
    }

    /// <inheritdoc />
    public override void Dispose ()
    {
        base.Dispose ();
        _adjustConsole?.Cleanup ();
    }
}