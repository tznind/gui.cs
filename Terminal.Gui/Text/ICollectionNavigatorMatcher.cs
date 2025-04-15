namespace Terminal.Gui;

/// <summary>
///     Determines which keys trigger collection manager navigation
///     and how to match typed strings to objects in the collection.
///     Default implementation is <see cref="DefaultCollectionNavigatorMatcher"/>.
/// </summary>
public interface ICollectionNavigatorMatcher
{
    /// <summary>
    ///     Returns true if <paramref name="a"/> is a searchable key (e.g. letters, numbers, etc) that are valid to pass
    ///     to this class for search filtering.
    /// </summary>
    /// <param name="a"></param>
    /// <returns></returns>
    bool IsCompatibleKey (Key a);

    /// <summary>
    ///     Return true if the <paramref name="value"/> matches (e.g. starts with)
    ///     the <paramref name="search"/> term.
    /// </summary>
    /// <param name="search"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    bool IsMatch (string search, object value);
}
