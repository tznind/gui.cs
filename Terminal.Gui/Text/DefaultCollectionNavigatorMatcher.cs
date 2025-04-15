#nullable enable

namespace Terminal.Gui;

public class DefaultCollectionNavigatorMatcher : ICollectionNavigatorMatcher
{

    /// <summary>The comparer function to use when searching the collection.</summary>
    public StringComparison Comparer { get; set; } = StringComparison.InvariantCultureIgnoreCase;

    /// <inheritdoc />
    public bool IsMatch (string search, object? value)
    {
        return value?.ToString ()?.StartsWith (search, Comparer) ?? false;
    }

    /// <summary>
    ///     Returns true if <paramref name="a"/> is a searchable key (e.g. letters, numbers, etc) that are valid to pass
    ///     to this class for search filtering.
    /// </summary>
    /// <param name="a"></param>
    /// <returns></returns>
    public bool IsCompatibleKey (Key a)
    {
        Rune rune = a.AsRune;

        return rune != default (Rune) && !Rune.IsControl (rune);
    }
}
