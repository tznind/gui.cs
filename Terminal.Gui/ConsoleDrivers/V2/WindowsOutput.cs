using System.ComponentModel;
using System.Runtime.InteropServices;
using static Terminal.Gui.WindowsConsole;

namespace Terminal.Gui;
public class WindowsOutput : IConsoleOutput
{

    [DllImport ("kernel32.dll", EntryPoint = "WriteConsole", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool WriteConsole (
        nint hConsoleOutput,
        string lpbufer,
        uint NumberOfCharsToWriten,
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
    private static extern bool GetConsoleScreenBufferInfoEx (nint hConsoleOutput, ref CONSOLE_SCREEN_BUFFER_INFOEX csbi);

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
    private static extern bool SetConsoleActiveScreenBuffer (nint Handle);

    [DllImport ("kernel32.dll")]
    private static extern bool SetConsoleCursorPosition (nint hConsoleOutput, Coord dwCursorPosition);

    private nint _screenBuffer;

    public WindowsConsole WinConsole { get; private set; } = new WindowsConsole (false);

    public WindowsOutput ()
    {
        _screenBuffer = CreateConsoleScreenBuffer (
                                                   DesiredAccess.GenericRead | DesiredAccess.GenericWrite,
                                                   ShareMode.FileShareRead | ShareMode.FileShareWrite,
                                                   nint.Zero,
                                                   1,
                                                   nint.Zero
                                                  );

        if (_screenBuffer == INVALID_HANDLE_VALUE)
        {
            int err = Marshal.GetLastWin32Error ();

            if (err != 0)
            {
                throw new Win32Exception (err);
            }
        }

        if (!SetConsoleActiveScreenBuffer (_screenBuffer))
        {
            throw new Win32Exception (Marshal.GetLastWin32Error ());
        }
    }
    public void Write (string str)
    {
        if (!WriteConsole (_screenBuffer, str, (uint)str.Length, out uint _, nint.Zero))
        {
            throw new Win32Exception (Marshal.GetLastWin32Error (), "Failed to write to console screen buffer.");
        }
    }


    public void Write (IOutputBuffer buffer)
    {

        var outputBuffer = new ExtendedCharInfo [buffer.Rows * buffer.Cols];

        Size windowSize = WinConsole?.GetConsoleBufferWindow (out Point _) ?? new Size (buffer.Cols, buffer.Rows);
        
        // TODO: probably do need this right?
        /*
        if (!windowSize.IsEmpty && (windowSize.Width != buffer.Cols || windowSize.Height != buffer.Rows))
        {
            return;
        }*/

        var bufferCoords = new Coord
        {
            X = (short)buffer.Cols, //Clip.Width,
            Y = (short)buffer.Rows, //Clip.Height
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

                if (buffer.Contents [row, col].Rune.IsBmp)
                {
                    outputBuffer [position].Char = (char)buffer.Contents [row, col].Rune.Value;
                }
                else
                {
                    //outputBuffer [position].Empty = true;
                    outputBuffer [position].Char = (char)Rune.ReplacementChar.Value;

                    if (buffer.Contents [row, col].Rune.GetColumns () > 1 && col + 1 < buffer.Cols)
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

        var damageRegion = new SmallRect
        {
            Top = 0,
            Left = 0,
            Bottom = (short)buffer.Rows,
            Right = (short)buffer.Cols
        };
        //size, ExtendedCharInfo [] charInfoBuffer, Coord , SmallRect window,
        if (WinConsole != null
            && !WinConsole.WriteToConsole (
                                           size: new (buffer.Cols, buffer.Rows),
                                           charInfoBuffer: outputBuffer,
                                           bufferSize: bufferCoords,
                                           window: damageRegion, false))
        {
            int err = Marshal.GetLastWin32Error ();

            if (err != 0)
            {
                throw new Win32Exception (err);
            }
        }

        SmallRect.MakeEmpty (ref damageRegion);
    }

    public Size GetWindowSize ()
    {
        var csbi = new CONSOLE_SCREEN_BUFFER_INFOEX ();
        csbi.cbSize = (uint)Marshal.SizeOf (csbi);

        if (!GetConsoleScreenBufferInfoEx (_screenBuffer, ref csbi))
        {
            //throw new System.ComponentModel.Win32Exception (Marshal.GetLastWin32Error ());
            return Size.Empty;
        }

        Size sz = new (
                       csbi.srWindow.Right - csbi.srWindow.Left + 1,
                       csbi.srWindow.Bottom - csbi.srWindow.Top + 1);
        return sz;
    }

    /// <inheritdoc />
    public void SetCursorVisibility (CursorVisibility visibility)
    {
        var sb = new StringBuilder ();
        sb.Append (visibility != CursorVisibility.Invisible ? EscSeqUtils.CSI_ShowCursor : EscSeqUtils.CSI_HideCursor);
        WinConsole?.WriteANSI (sb.ToString ());
    }

    /// <inheritdoc />
    public void SetCursorPosition (int col, int row)
    {
        SetConsoleCursorPosition (_screenBuffer, new Coord ((short)col, (short)row));
    }

    /// <inheritdoc />
    public void Dispose ()
    {
        if (_screenBuffer != nint.Zero)
        {
            CloseHandle (_screenBuffer);
        }
    }
}
