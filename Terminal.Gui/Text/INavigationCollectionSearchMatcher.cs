namespace Terminal.Gui;

public interface INavigationCollectionSearchMatcher
{
    /// <summary>
    ///     Returns true if <paramref name="a"/> is a searchable key (e.g. letters, numbers, etc) that are valid to pass
    ///     to this class for search filtering.
    /// </summary>
    /// <param name="a"></param>
    /// <returns></returns>
    bool IsCompatibleKey (Key a);

    bool IsMatch (string search, object value);
}
