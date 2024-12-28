using Microsoft.Extensions.Logging;

namespace Terminal.Gui;

public class NetInput : ConsoleInput<ConsoleKeyInfo>, INetInput
{
    private readonly NetWinVTConsole _adjustConsole;

    public NetInput ()
    {
        Logging.Logger.LogInformation ($"Creating {nameof (NetInput)}");
        PlatformID p = Environment.OSVersion.Platform;

        if (p == PlatformID.Win32NT || p == PlatformID.Win32S || p == PlatformID.Win32Windows)
        {
            try
            {
                _adjustConsole = new ();
            }
            catch (ApplicationException ex)
            {
                // Likely running as a unit test, or in a non-interactive session.
                Logging.Logger.LogCritical (
                                            ex,
                                            "NetWinVTConsole could not be constructed i.e. could not configure terminal modes. May indicate running in non-interactive session e.g. unit testing CI");
            }
        }

        // Doesn't seem to work
        Console.Out.Write (EscSeqUtils.CSI_EnableMouseEvents);
    }

    /// <inheritdoc/>
    protected override bool Peek () { return Console.KeyAvailable; }

    /// <inheritdoc/>
    protected override IEnumerable<ConsoleKeyInfo> Read ()
    {
        while (Console.KeyAvailable)
        {
            yield return Console.ReadKey (true);
        }
    }

    /// <inheritdoc/>
    public override void Dispose ()
    {
        base.Dispose ();
        _adjustConsole?.Cleanup ();
    }
}
