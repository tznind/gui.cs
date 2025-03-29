#nullable enable
using System.Globalization;

namespace Terminal.Gui;

/// <summary>
///     Provides a mapping between <see cref="Color"/> and the W3C standard color name strings.
/// </summary>
public static class ColorStrings
{
    private static readonly AnsiColorNameResolver Ansi = new();
    private static readonly W3cColorNameResolver W3c = new();
    private static readonly MultiStandardColorNameResolver Multi = new();

    /// <summary>
    ///     Gets the W3C standard string for <paramref name="color"/>.
    /// </summary>
    /// <param name="color">The color.</param>
    /// <returns><see langword="null"/> if there is no standard color name for the specified color.</returns>
    public static string? GetW3CColorName (Color color)
    {
        if (W3c.TryNameColor (color, out string? name))
        {
            return name;
        }
        return null;
    }

    /// <summary>
    ///     Gets the ANSI 4-bit (16) color name for <paramref name="color"/>.
    /// </summary>
    /// <param name="color">The color.</param>
    /// <returns><see langword="null"/> if there is no standard color name for the specified color.</returns>
    public static string? GetANSIColor16Name (Color color)
    {
        if (Ansi.TryNameColor (color, out string? name))
        {
            return name;
        }
        return null;
    }

    /// <summary>
    ///     Gets backwards compatible color name for <paramref name="color"/>.
    /// </summary>
    /// <param name="color">The color.</param>
    /// <returns>Standard color name for the specified color; otherwise <see langword="null"/>.</returns>
    public static string? GetColorName (Color color)
    {
        if (Multi.TryNameColor (color, out string? name))
        {
            return name;
        }
        return null;
    }

    /// <summary>
    ///     Returns the list of W3C standard color names.
    /// </summary>
    /// <returns></returns>
    public static IEnumerable<string> GetW3CColorNames ()
    {
        return W3c.GetColorNames ();
    }

    /// <summary>
    ///     Parses <paramref name="name"/> and returns <paramref name="color"/> if name is a W3C standard named color.
    /// </summary>
    /// <param name="name">The name to parse.</param>
    /// <param name="color">If successful, the color.</param>
    /// <returns><see langword="true"/> if <paramref name="name"/> was parsed successfully.</returns>
    public static bool TryParseW3CColorName (ReadOnlySpan<char> name, out Color color)
    {
        if (W3c.TryParseColor (name, out color))
        {
            return true;
        }
        // Backwards compatibility: Also parse #RRGGBB.
        return TryParseHexColor (name, out color);
    }

    /// <summary>
    ///     Parses <paramref name="name"/> and returns <paramref name="color"/> if name is a ANSI 4-bit standard named color.
    /// </summary>
    /// <param name="name">The name to parse.</param>
    /// <param name="color">If successful, the color.</param>
    /// <returns><see langword="true"/> if <paramref name="name"/> was parsed successfully.</returns>
    public static bool TryParseColor16 (ReadOnlySpan<char> name, out Color color)
    {
        if (Ansi.TryParseColor (name, out color))
        {
            return true;
        }
        color = default;
        return false;
    }

    /// <summary>
    ///     Parses <paramref name="name"/> and returns <paramref name="color"/> if name is either ANSI 4-bit or W3C standard named color.
    /// </summary>
    /// <param name="name">The name to parse.</param>
    /// <param name="color">If successful, the color.</param>
    /// <returns><see langword="true"/> if <paramref name="name"/> was parsed successfully.</returns>
    public static bool TryParseNamedColor (ReadOnlySpan<char> name, out Color color)
    {
        if (Multi.TryParseColor (name, out color))
        {
            return true;
        }
        // Backwards compatibility: Also parse #RRGGBB.
        if (TryParseHexColor (name, out color))
        {
            return true;
        }

        color = default;
        return false;
    }

    private static bool TryParseHexColor (ReadOnlySpan<char> name, out Color color)
    {
        if (name.Length == 7 && name [0] == '#')
        {
            if (int.TryParse (name.Slice (1, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int r) &&
                int.TryParse (name.Slice (3, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int g) &&
                int.TryParse (name.Slice (5, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int b))
            {
                color = new Color (r, g, b);
                return true;
            }
        }

        color = default;
        return false;
    }
}
