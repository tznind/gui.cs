#nullable enable
using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

namespace Terminal.Gui.Drivers;

internal partial class WindowsOutput : IConsoleOutput
{
    [LibraryImport ("kernel32.dll", EntryPoint = "WriteConsoleW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    [return: MarshalAs (UnmanagedType.Bool)]
    private static partial bool WriteConsole (
        nint hConsoleOutput,
        ReadOnlySpan<char> lpbufer,
        uint numberOfCharsToWriten,
        out uint lpNumberOfCharsWritten,
        nint lpReserved
    );

    [DllImport ("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle (nint handle);

    [DllImport ("kernel32.dll", SetLastError = true)]
    private static extern nint CreateConsoleScreenBuffer (
        DesiredAccess dwDesiredAccess,
        ShareMode dwShareMode,
        nint secutiryAttributes,
        uint flags,
        nint screenBufferData
    );

    [DllImport ("kernel32.dll", SetLastError = true)]
    private static extern bool GetConsoleScreenBufferInfoEx (nint hConsoleOutput, ref WindowsConsole.CONSOLE_SCREEN_BUFFER_INFOEX csbi);

    [Flags]
    private enum ShareMode : uint
    {
        FileShareRead = 1,
        FileShareWrite = 2
    }

    [Flags]
    private enum DesiredAccess : uint
    {
        GenericRead = 2147483648,
        GenericWrite = 1073741824
    }

    internal static nint INVALID_HANDLE_VALUE = new (-1);

    [DllImport ("kernel32.dll", SetLastError = true)]
    private static extern bool SetConsoleActiveScreenBuffer (nint handle);

    [DllImport ("kernel32.dll")]
    private static extern bool SetConsoleCursorPosition (nint hConsoleOutput, WindowsConsole.Coord dwCursorPosition);

    [DllImport ("kernel32.dll", SetLastError = true)]
    private static extern bool SetConsoleCursorInfo (nint hConsoleOutput, [In] ref WindowsConsole.ConsoleCursorInfo lpConsoleCursorInfo);

    [DllImport ("kernel32.dll", SetLastError = true)]
    public static extern bool SetConsoleTextAttribute (nint hConsoleOutput, ushort wAttributes);

    private nint _screenBuffer;

    // Last text style used, for updating style with EscSeqUtils.CSI_AppendTextStyleChange().
    private TextStyle _redrawTextStyle = TextStyle.None;

    public WindowsOutput ()
    {
        Logging.Logger.LogInformation ($"Creating {nameof (WindowsOutput)}");

        if (ConsoleDriver.RunningUnitTests)
        {
            return;
        }

        _screenBuffer = CreateScreenBuffer ();

        var backBuffer = CreateScreenBuffer ();
        _doubleBuffer = [_screenBuffer, backBuffer];

        if (!SetConsoleActiveScreenBuffer (_screenBuffer))
        {
            throw new Win32Exception (Marshal.GetLastWin32Error ());
        }
    }

    private int _activeDoubleBuffer = 0;
    private nint [] _doubleBuffer = new nint[2];

    private nint CreateScreenBuffer ()
    {
        var buff = CreateConsoleScreenBuffer (
                                   DesiredAccess.GenericRead | DesiredAccess.GenericWrite,
                                   ShareMode.FileShareRead | ShareMode.FileShareWrite,
                                   nint.Zero,
                                   1,
                                   nint.Zero
                                  );

        if (buff == INVALID_HANDLE_VALUE)
        {
            int err = Marshal.GetLastWin32Error ();

            if (err != 0)
            {
                throw new Win32Exception (err);
            }
        }

        return buff;
    }

    public void Write (ReadOnlySpan<char> str)
    {
        if (!WriteConsole (_screenBuffer, str, (uint)str.Length, out uint _, nint.Zero))
        {
            throw new Win32Exception (Marshal.GetLastWin32Error (), "Failed to write to console screen buffer.");
        }
    }

    public void Write (IOutputBuffer outputBuffer)
    {
        bool force16Colors = Application.Driver!.Force16Colors;

        // for 16 color mode we will write to a backing buffer then flip it to the active one at the end to avoid jitter.
        nint consoleBuffer;
        if (force16Colors)
        {
            consoleBuffer = _doubleBuffer [_activeDoubleBuffer = (_activeDoubleBuffer + 1) % 2];
            _screenBuffer = consoleBuffer;
        }
        else
        {
            consoleBuffer = _screenBuffer;
        }

        var result = false;

        GetWindowSize (out var originalCursorPosition);

        StringBuilder stringBuilder = new ();

        Attribute? prev = null;

        for (var row = 0; row < outputBuffer.Rows; row++)
        {
            StringBuilder sbSinceLastAttrChange = new StringBuilder ();

            AppendOrWriteCursorPosition (new (0, row), force16Colors, stringBuilder, consoleBuffer);

            for (var col = 0; col < outputBuffer.Cols; col++)
            {
                var cell = outputBuffer.Contents [row, col];
                var attr = cell.Attribute ?? prev ?? new ();

                // If we have 10 width and are at 9 it's ok
                // But if it's width 2 we can't write it so must skip
                if (cell.Rune.GetColumns () + col > outputBuffer.Cols)
                {
                    continue;
                }

                if (attr != prev)
                {
                    prev = attr;

                    // write everything out up till now
                    AppendOrWrite (sbSinceLastAttrChange.ToString (), force16Colors, stringBuilder, consoleBuffer);

                    // then change color/style etc
                    AppendOrWrite (attr!, force16Colors, stringBuilder, (nint)consoleBuffer);
                    sbSinceLastAttrChange.Clear ();
                    sbSinceLastAttrChange.Append (cell.Rune);
                    _redrawTextStyle = attr.Style;
                }
                else
                {
                    sbSinceLastAttrChange.Append (cell.Rune);
                }
            }

            // write trailing bits
            if (sbSinceLastAttrChange.Length > 0)
            {
                AppendOrWrite (sbSinceLastAttrChange.ToString (), force16Colors, stringBuilder, consoleBuffer);
                sbSinceLastAttrChange.Clear ();
            }
        }

        AppendOrWriteCursorPosition(new (originalCursorPosition.X, originalCursorPosition.Y),force16Colors,stringBuilder, consoleBuffer);

        if (force16Colors)
        {
            SetConsoleActiveScreenBuffer (consoleBuffer);
            return;
        }

        var span = stringBuilder.ToString ().AsSpan (); // still allocates the string
        result = WriteConsole (consoleBuffer, span, (uint)span.Length, out _, nint.Zero);

        foreach (SixelToRender sixel in Application.Sixel)
        {
            SetCursorPosition ((short)sixel.ScreenPosition.X, (short)sixel.ScreenPosition.Y);
            WriteConsole (consoleBuffer, sixel.SixelData, (uint)sixel.SixelData.Length, out uint _, nint.Zero);
        }

        if (!result)
        {
            int err = Marshal.GetLastWin32Error ();

            if (err != 0)
            {
                throw new Win32Exception (err);
            }
        }
    }


    private void AppendOrWrite (string str, bool force16Colors, StringBuilder stringBuilder, nint screenBuffer)
    {
        if (str.Length == 0)
        {
            return;
        }

        // Replace escape characters with space
        str = str.Replace ("\x1b", " ");


        if (force16Colors)
        {
            var a = str.ToCharArray ();
            WriteConsole (screenBuffer,a ,(uint)a.Length, out _, nint.Zero);
        }
        else
        {
            stringBuilder.Append (str);
        }
    }

    private void AppendOrWrite (Attribute attr, bool force16Colors, StringBuilder stringBuilder, nint screenBuffer)
    {
        if (force16Colors)
        {
            var as16ColorInt = (ushort)((int)attr.Foreground.GetClosestNamedColor16 () | ((int)attr.Background.GetClosestNamedColor16 () << 4));
            SetConsoleTextAttribute (screenBuffer, as16ColorInt);
        }
        else
        {
            EscSeqUtils.CSI_AppendForegroundColorRGB (stringBuilder, attr.Foreground.R, attr.Foreground.G, attr.Foreground.B);
            EscSeqUtils.CSI_AppendBackgroundColorRGB (stringBuilder, attr.Background.R, attr.Background.G, attr.Background.B);
            EscSeqUtils.CSI_AppendTextStyleChange (stringBuilder, _redrawTextStyle, attr.Style);
        }
    }

    private void AppendOrWriteCursorPosition (Point p, bool force16Colors, StringBuilder stringBuilder, nint screenBuffer)
    {
        if (force16Colors)
        {
            SetConsoleCursorPosition (screenBuffer, new ((short)p.X, (short)p.Y));
        }
        else
        {
            // CSI codes are 1 indexed
            stringBuilder.Append (EscSeqUtils.CSI_SaveCursorPosition);
            EscSeqUtils.CSI_AppendCursorPosition (stringBuilder, p.Y+1, p.X+1);
        }
    }

    public Size GetWindowSize ()
    {
        return GetWindowSize (out _);
    }
    public Size GetWindowSize (out WindowsConsole.Coord cursorPosition)
    {
        var csbi = new WindowsConsole.CONSOLE_SCREEN_BUFFER_INFOEX ();
        csbi.cbSize = (uint)Marshal.SizeOf (csbi);

        if (!GetConsoleScreenBufferInfoEx (_screenBuffer, ref csbi))
        {
            //throw new System.ComponentModel.Win32Exception (Marshal.GetLastWin32Error ());
            cursorPosition = default;
            return Size.Empty;
        }

        Size sz = new (
                       csbi.srWindow.Right - csbi.srWindow.Left + 1,
                       csbi.srWindow.Bottom - csbi.srWindow.Top + 1);

        cursorPosition = csbi.dwCursorPosition;
        return sz;
    }

    /// <inheritdoc/>
    public void SetCursorVisibility (CursorVisibility visibility)
    {
        if (ConsoleDriver.RunningUnitTests)
        {
            return;
        }

        if (Application.Driver!.Force16Colors)
        {
            var info = new WindowsConsole.ConsoleCursorInfo
            {
                dwSize = (uint)visibility & 0x00FF,
                bVisible = ((uint)visibility & 0xFF00) != 0
            };

            SetConsoleCursorInfo (_screenBuffer, ref info);
        }
        else
        {
            string cursorVisibilitySequence = visibility != CursorVisibility.Invisible
                                                  ? EscSeqUtils.CSI_ShowCursor
                                                  : EscSeqUtils.CSI_HideCursor;
            Write (cursorVisibilitySequence);
        }
    }

    private Point _lastCursorPosition;

    /// <inheritdoc/>
    public void SetCursorPosition (int col, int row)
    {
        if (_lastCursorPosition.X == col && _lastCursorPosition.Y == row)
        {
            return;
        }

        _lastCursorPosition = new (col, row);

        SetConsoleCursorPosition (_screenBuffer, new ((short)col, (short)row));
    }

    private bool _isDisposed;

    /// <inheritdoc/>
    public void Dispose ()
    {
        if (_isDisposed)
        {
            return;
        }

        for (int i = 0; i < 2; i++)
        {
            var buffer = _doubleBuffer [i];
            if (buffer != nint.Zero)
            {
                try
                {
                    CloseHandle (buffer);
                }
                catch (Exception e)
                {
                    Logging.Logger.LogError (e, "Error trying to close screen buffer handle in WindowsOutput via interop method");
                }
            }
        }

        _isDisposed = true;
    }
}
