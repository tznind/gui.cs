#nullable enable
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Terminal.Gui.Drivers;

internal partial class WindowsOutput : OutputBase, IConsoleOutput
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
    private static extern nint GetStdHandle (int nStdHandle);

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

    [DllImport ("kernel32.dll")]
    private static extern bool GetConsoleMode (nint hConsoleHandle, out uint lpMode);

    private const int STD_OUTPUT_HANDLE = -11;
    private const uint ENABLE_VIRTUAL_TERMINAL_PROCESSING = 0x0004;
    private readonly nint _outputHandle;
    private nint _screenBuffer;
    private readonly bool _isVirtualTerminal;
    
    public WindowsOutput ()
    {
        Logging.Logger.LogInformation ($"Creating {nameof (WindowsOutput)}");

        if (ConsoleDriver.RunningUnitTests)
        {
            return;
        }

        _outputHandle = GetStdHandle (STD_OUTPUT_HANDLE);
        _isVirtualTerminal = GetConsoleMode (_outputHandle, out uint mode) && (mode & ENABLE_VIRTUAL_TERMINAL_PROCESSING) != 0;

        if (_isVirtualTerminal)
        {
            //Enable alternative screen buffer.
            Console.Out.Write (EscSeqUtils.CSI_SaveCursorAndActivateAltBufferNoBackscroll);
        }
        else
        {
            _screenBuffer = CreateScreenBuffer ();

            var backBuffer = CreateScreenBuffer ();
            _doubleBuffer = [_screenBuffer, backBuffer];

            if (!SetConsoleActiveScreenBuffer (_screenBuffer))
            {
                throw new Win32Exception (Marshal.GetLastWin32Error ());
            }

            // Force 16 colors if not in virtual terminal mode.
            Application.Force16Colors = true;
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
        if (!WriteConsole (_isVirtualTerminal ? _outputHandle : _screenBuffer, str, (uint)str.Length, out uint _, nint.Zero))
        {
            throw new Win32Exception (Marshal.GetLastWin32Error (), "Failed to write to console screen buffer.");
        }
    }

    public void RecreateBackBuffer ()
    {
        int idx = (_activeDoubleBuffer + 1) % 2;
        var inactiveBuffer = _doubleBuffer [idx];

        DisposeBuffer (inactiveBuffer);
        _doubleBuffer [idx] = CreateScreenBuffer ();
    }

    private void DisposeBuffer (nint buffer)
    {
        if (buffer != 0 && buffer != INVALID_HANDLE_VALUE)
        {
            if (!CloseHandle (buffer))
            {
                throw new Win32Exception (Marshal.GetLastWin32Error (), "Failed to close screen buffer handle.");
            }
        }
    }

    public override void Write (IOutputBuffer outputBuffer)
    {
        _force16Colors = Application.Driver!.Force16Colors;
        _everythingStringBuilder = new StringBuilder ();

        // for 16 color mode we will write to a backing buffer then flip it to the active one at the end to avoid jitter.
        _consoleBuffer = 0;
        if (_force16Colors)
        {
            if (_isVirtualTerminal)
            {
                _consoleBuffer = _outputHandle;
            }
            else
            {
                _consoleBuffer = _doubleBuffer [_activeDoubleBuffer = (_activeDoubleBuffer + 1) % 2];
                _screenBuffer = _consoleBuffer;
            }
        }
        else
        {
            _consoleBuffer = _outputHandle;
        }

        base.Write (outputBuffer);
        /*

        var result = false;

        GetWindowSize (out var originalCursorPosition);

        StringBuilder stringBuilder = new ();

        Attribute? prev = null;

        for (var row = 0; row < outputBuffer.Rows; row++)
        {
            StringBuilder sbSinceLastAttrChange = new StringBuilder ();

            AppendOrWriteCursorPosition (new (0, row), _force16Colors, stringBuilder, _consoleBuffer);

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
                    AppendOrWrite (sbSinceLastAttrChange.ToString (), _force16Colors, stringBuilder, _consoleBuffer);

                    // then change color/style etc
                    AppendOrWrite (attr!, _force16Colors, stringBuilder, (nint)_consoleBuffer);
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
                AppendOrWrite (sbSinceLastAttrChange.ToString (), force16Colors, stringBuilder, _consoleBuffer);
                sbSinceLastAttrChange.Clear ();
            }
        }

        AppendOrWriteCursorPosition(new (originalCursorPosition.X, originalCursorPosition.Y),force16Colors,stringBuilder, _consoleBuffer);

        if (force16Colors && !_isVirtualTerminal)
        {
            SetConsoleActiveScreenBuffer (_consoleBuffer);
            return;
        }

        var span = stringBuilder.ToString ().AsSpan (); // still allocates the string
        result = WriteConsole (_consoleBuffer, span, (uint)span.Length, out _, nint.Zero);

        foreach (SixelToRender sixel in Application.Sixel)
        {
            SetCursorPosition ((short)sixel.ScreenPosition.X, (short)sixel.ScreenPosition.Y);
            WriteConsole (_consoleBuffer, sixel.SixelData, (uint)sixel.SixelData.Length, out uint _, nint.Zero);
        }

        */

        if (_force16Colors && !_isVirtualTerminal)
        {
            SetConsoleActiveScreenBuffer (_consoleBuffer);
        }
        else
        {
            var span = _everythingStringBuilder.ToString ().AsSpan (); // still allocates the string

            var result = WriteConsole (_consoleBuffer, span, (uint)span.Length, out _, nint.Zero);
            if (!result)
            {
                int err = Marshal.GetLastWin32Error ();

                if (err != 0)
                {
                    throw new Win32Exception (err);
                }
            }
        }
    }
    /// <inheritdoc />
    protected override void Write (StringBuilder output)
    {
        if (output.Length == 0)
        {
            return;
        }

        var str = output.ToString ();

        if (_force16Colors && !_isVirtualTerminal)
        {
            var a = str.ToCharArray ();
            WriteConsole (_screenBuffer,a ,(uint)a.Length, out _, nint.Zero);
        }
        else
        {
            _everythingStringBuilder.Append (str);
        }
    }

    /// <inheritdoc />
    protected override void AppendOrWriteAttribute (StringBuilder output, Attribute attr, TextStyle redrawTextStyle)
    {
        var force16Colors = Application.Force16Colors;

        if (force16Colors)
        {
            if (_isVirtualTerminal)
            {
                output.Append (EscSeqUtils.CSI_SetForegroundColor (attr.Foreground.GetAnsiColorCode ()));
                output.Append (EscSeqUtils.CSI_SetBackgroundColor (attr.Background.GetAnsiColorCode ()));
                EscSeqUtils.CSI_AppendTextStyleChange (output, redrawTextStyle, attr.Style);
            }
            else
            {
                var as16ColorInt = (ushort)((int)attr.Foreground.GetClosestNamedColor16 () | ((int)attr.Background.GetClosestNamedColor16 () << 4));
                SetConsoleTextAttribute (_screenBuffer, as16ColorInt);
            }
        }
        else
        {
            EscSeqUtils.CSI_AppendForegroundColorRGB (output, attr.Foreground.R, attr.Foreground.G, attr.Foreground.B);
            EscSeqUtils.CSI_AppendBackgroundColorRGB (output, attr.Background.R, attr.Background.G, attr.Background.B);
            EscSeqUtils.CSI_AppendTextStyleChange (output, redrawTextStyle, attr.Style);
        }
    }


    private Size? _lastSize = null;
    public Size GetWindowSize ()
    {

        var newSize = GetWindowSize (out _);

        if (_lastSize == null || _lastSize != newSize)
        {
            _lastSize ??= newSize;

            // Back buffers only apply to 16 color mode so if not in that just ignore
            if (_isVirtualTerminal)
            {
                return newSize;
            }

            // User is resizing the screen, they can only ever resize the active
            // buffer since. We now however have issue because background offscreen
            // buffer will be wrong size, recreate it to ensure it doesn't result in
            // differing active and back buffer sizes (which causes flickering of window size)
            RecreateBackBuffer ();
        }

        return newSize;
    }

    public Size GetWindowSize (out WindowsConsole.Coord cursorPosition)
    {
        var csbi = new WindowsConsole.CONSOLE_SCREEN_BUFFER_INFOEX ();
        csbi.cbSize = (uint)Marshal.SizeOf (csbi);

        if (!GetConsoleScreenBufferInfoEx (_isVirtualTerminal ? _outputHandle : _screenBuffer, ref csbi))
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


    /// <inheritdoc />
    protected override bool SetCursorPositionImpl (int screenPositionX, int screenPositionY)
    {

        if (_force16Colors && !_isVirtualTerminal)
        {
            SetConsoleCursorPosition (_screenBuffer, new ((short)screenPositionX, (short)screenPositionY));
        }
        else
        {
            // CSI codes are 1 indexed
            _everythingStringBuilder.Append (EscSeqUtils.CSI_SaveCursorPosition);
            EscSeqUtils.CSI_AppendCursorPosition (_everythingStringBuilder, screenPositionY + 1, screenPositionX + 1);
        }

        return true;
    }

    /// <inheritdoc/>
    public override void SetCursorVisibility (CursorVisibility visibility)
    {
        if (ConsoleDriver.RunningUnitTests)
        {
            return;
        }

        if (!_isVirtualTerminal)
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
    private bool _force16Colors;
    private nint _consoleBuffer;
    private StringBuilder _everythingStringBuilder;

    /// <inheritdoc/>
    public void Dispose ()
    {
        if (_isDisposed)
        {
            return;
        }

        if (_isVirtualTerminal)
        {
            //Disable alternative screen buffer.
            Console.Out.Write (EscSeqUtils.CSI_RestoreCursorAndRestoreAltBufferWithBackscroll);
        }
        else
        {
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
        }

        _isDisposed = true;
    }
}
