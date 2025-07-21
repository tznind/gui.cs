#nullable enable
using System.Buffers;
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

    private readonly nint _screenBuffer;

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

    public void Write (IOutputBuffer buffer)
    {
        WindowsConsole.ExtendedCharInfo [] outputBuffer = new WindowsConsole.ExtendedCharInfo [buffer.Rows * buffer.Cols];

        // TODO: probably do need this right?
        /*
        if (!windowSize.IsEmpty && (windowSize.Width != buffer.Cols || windowSize.Height != buffer.Rows))
        {
            return;
        }*/

        var bufferCoords = new WindowsConsole.Coord
        {
            X = (short)buffer.Cols, //Clip.Width,
            Y = (short)buffer.Rows //Clip.Height
        };

        for (var row = 0; row < buffer.Rows; row++)
        {
            if (!buffer.DirtyLines [row])
            {
                continue;
            }

            buffer.DirtyLines [row] = false;

            for (var col = 0; col < buffer.Cols; col++)
            {
                int position = row * buffer.Cols + col;
                outputBuffer [position].Attribute = buffer.Contents [row, col].Attribute.GetValueOrDefault ();

                if (buffer.Contents [row, col].IsDirty == false)
                {
                    outputBuffer [position].Empty = true;
                    outputBuffer [position].Char = (char)Rune.ReplacementChar.Value;

                    continue;
                }

                outputBuffer [position].Empty = false;

                Rune rune = buffer.Contents [row, col].Rune;
                int width = rune.GetColumns ();

                if (rune.IsBmp)
                {
                    if (width == 1)
                    {
                        // Single-width char, just encode first UTF-16 char
                        outputBuffer [position].Char = (char)rune.Value;
                    }
                    else if (width == 2 && col + 1 < buffer.Cols)
                    {
                        // Double-width char: encode to UTF-16 surrogate pair and write both halves
                        var utf16 = new char [2];
                        rune.EncodeToUtf16 (utf16);
                        outputBuffer [position].Char = utf16 [0];
                        outputBuffer [position].Empty = false;

                        // Write second half into next cell
                        col++;
                        position = row * buffer.Cols + col;
                        outputBuffer [position].Char = utf16 [1];
                        outputBuffer [position].Empty = false;
                    }
                }
                else
                {
                    //outputBuffer [position].Empty = true;
                    outputBuffer [position].Char = (char)Rune.ReplacementChar.Value;

                    if (width > 1 && col + 1 < buffer.Cols)
                    {
                        // TODO: This is a hack to deal with non-BMP and wide characters.
                        col++;
                        position = row * buffer.Cols + col;
                        outputBuffer [position].Empty = false;
                        outputBuffer [position].Char = ' ';
                    }
                }
            }
        }

        var damageRegion = new WindowsConsole.SmallRect
        {
            Top = 0,
            Left = 0,
            Bottom = (short)buffer.Rows,
            Right = (short)buffer.Cols
        };

        //size, ExtendedCharInfo [] charInfoBuffer, Coord , SmallRect window,
        if (!ConsoleDriver.RunningUnitTests
            && !WriteToConsole (
                                new (buffer.Cols, buffer.Rows),
                                outputBuffer,
                                bufferCoords,
                                damageRegion,
                                Application.Driver!.Force16Colors))
        {
            int err = Marshal.GetLastWin32Error ();

            if (err != 0)
            {
                throw new Win32Exception (err);
            }
        }

        WindowsConsole.SmallRect.MakeEmpty (ref damageRegion);
    }

    public bool WriteToConsole (Size size, WindowsConsole.ExtendedCharInfo [] charInfoBuffer, WindowsConsole.Coord bufferSize, WindowsConsole.SmallRect window, bool force16Colors)
    {
        // for 16 color mode we will write to a backing buffer then flip it to the active one at the end to avoid jitter.
        var buffer = force16Colors ? _doubleBuffer[_activeDoubleBuffer = (_activeDoubleBuffer + 1) % 2] : _screenBuffer;

        var result = false;

        GetWindowSize (out var cursorPosition);

        if (force16Colors)
        {
            SetConsoleCursorPosition (buffer, new (0, 0));

        }

        StringBuilder stringBuilder = new();

        stringBuilder.Append (EscSeqUtils.CSI_SaveCursorPosition);
        EscSeqUtils.CSI_AppendCursorPosition (stringBuilder, 0, 0);

        Attribute? prev = null;

        foreach (WindowsConsole.ExtendedCharInfo info in charInfoBuffer)
        {
            Attribute attr = info.Attribute;

            if (attr != prev)
            {
                prev = attr;
                AppendOrWrite (attr,force16Colors,stringBuilder,buffer);
                _redrawTextStyle = attr.Style;
            }


            if (info.Char != '\x1b')
            {
                if (!info.Empty)
                {

                    AppendOrWrite (info.Char, force16Colors, stringBuilder, buffer);
                }
            }
            else
            {
                stringBuilder.Append (' ');
            }
        }

        if (force16Colors)
        {
            SetConsoleActiveScreenBuffer (buffer);
            SetConsoleCursorPosition (buffer, new (cursorPosition.X, cursorPosition.Y));
            return true;
        }

        stringBuilder.Append (EscSeqUtils.CSI_RestoreCursorPosition);
        stringBuilder.Append (EscSeqUtils.CSI_HideCursor);

        // TODO: Potentially could stackalloc whenever reasonably small (<= 8 kB?) write buffer is needed.
        char [] rentedWriteArray = ArrayPool<char>.Shared.Rent (minimumLength: stringBuilder.Length);
        try
        {
            Span<char> writeBuffer = rentedWriteArray.AsSpan(0, stringBuilder.Length);
            stringBuilder.CopyTo (0, writeBuffer, stringBuilder.Length);

            // Supply console with the new content.
            result = WriteConsole (_screenBuffer, writeBuffer, (uint)writeBuffer.Length, out uint _, nint.Zero);
        }
        finally
        {
            ArrayPool<char>.Shared.Return (rentedWriteArray);
        }

        foreach (SixelToRender sixel in Application.Sixel)
        {
            SetCursorPosition ((short)sixel.ScreenPosition.X, (short)sixel.ScreenPosition.Y);
            WriteConsole (_screenBuffer, sixel.SixelData, (uint)sixel.SixelData.Length, out uint _, nint.Zero);
        }

        if (!result)
        {
            int err = Marshal.GetLastWin32Error ();

            if (err != 0)
            {
                throw new Win32Exception (err);
            }
        }

        return result;
    }

    private void AppendOrWrite (char infoChar, bool force16Colors, StringBuilder stringBuilder, nint screenBuffer)
    {

        if (force16Colors)
        {
            WriteConsole (screenBuffer, [infoChar],1, out _, nint.Zero);
        }
        else
        {
            stringBuilder.Append (infoChar);
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

        if (_screenBuffer != nint.Zero)
        {
            try
            {
                CloseHandle (_screenBuffer);
            }
            catch (Exception e)
            {
                Logging.Logger.LogError (e, "Error trying to close screen buffer handle in WindowsOutput via interop method");
            }
        }

        _isDisposed = true;
    }
}
