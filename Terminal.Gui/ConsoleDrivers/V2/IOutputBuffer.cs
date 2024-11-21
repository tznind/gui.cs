namespace Terminal.Gui;

public interface IOutputBuffer
{
    int Rows { get;}
    int Cols { get; }

    /// <summary>
    /// As performance is a concern, we keep track of the dirty lines and only refresh those.
    /// This is in addition to the dirty flag on each cell.
    /// </summary>
    public bool [] DirtyLines { get; }

    /// <summary>
    ///     The contents of the application output. The driver outputs this buffer to the terminal when
    ///     UpdateScreen is called.
    ///     <remarks>The format of the array is rows, columns. The first index is the row, the second index is the column.</remarks>
    /// </summary>
    public Cell [,] Contents { get;}
}
