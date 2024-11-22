namespace Terminal.Gui;

public interface IOutputBuffer
{
    /// <summary>
    /// As performance is a concern, we keep track of the dirty lines and only refresh those.
    /// This is in addition to the dirty flag on each cell.
    /// </summary>
    public bool [] DirtyLines { get; }

    /// <summary>
    /// The contents of the application output. The driver outputs this buffer to the terminal when UpdateScreen is called.
    /// </summary>
    Cell [,] Contents { get; set; }

    /// <summary>
    /// The <see cref="Attribute"/> that will be used for the next AddRune or AddStr call.
    /// </summary>
    Attribute CurrentAttribute { get; set; }

    /// <summary>The number of rows visible in the terminal.</summary>
    int Rows { get; set; }

    /// <summary>The number of columns visible in the terminal.</summary>
    int Cols { get; set; }

    /// <summary>
    ///     Gets the row last set by <see cref="Move"/>. <see cref="Col"/> and <see cref="Row"/> are used by
    ///     <see cref="AddRune(Rune)"/> and <see cref="AddStr"/> to determine where to add content.
    /// </summary>
    public int Row { get;}


    /// <summary>
    ///     Gets the column last set by <see cref="Move"/>. <see cref="Col"/> and <see cref="Row"/> are used by
    ///     <see cref="AddRune(Rune)"/> and <see cref="AddStr"/> to determine where to add content.
    /// </summary>
    public int Col { get; }
    /// <summary>
    /// Updates the column and row to the specified location in the buffer.
    /// </summary>
    /// <param name="col">The column to move to.</param>
    /// <param name="row">The row to move to.</param>
    void Move (int col, int row);

    /// <summary>Adds the specified rune to the display at the current cursor position.</summary>
    /// <param name="rune">Rune to add.</param>
    void AddRune (Rune rune);

    /// <summary>
    /// Adds the specified character to the display at the current cursor position. This is a convenience method for AddRune.
    /// </summary>
    /// <param name="c">Character to add.</param>
    void AddRune (char c);

    /// <summary>Adds the string to the display at the current cursor position.</summary>
    /// <param name="str">String to add.</param>
    void AddStr (string str);

    /// <summary>Clears the contents of the buffer.</summary>
    void ClearContents ();

    /// <summary>
    /// Tests whether the specified coordinate is valid for drawing the specified Rune.
    /// </summary>
    /// <param name="rune">Used to determine if one or two columns are required.</param>
    /// <param name="col">The column.</param>
    /// <param name="row">The row.</param>
    /// <returns>
    /// True if the coordinate is valid for the Rune; false otherwise.
    /// </returns>
    bool IsValidLocation (Rune rune, int col, int row);

    /// <summary>
    /// Changes the size of the buffer to the given size
    /// </summary>
    /// <param name="cols"></param>
    /// <param name="rows"></param>
    void SetWindowSize (int cols, int rows);
}
