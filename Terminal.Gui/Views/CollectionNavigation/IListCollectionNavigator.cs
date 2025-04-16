using System.Collections;

namespace Terminal.Gui;

/// <summary>
///     <see cref="ICollectionNavigator"/> sub-interface for <see cref="ListView"/>. See also
///     <see cref="ListView.KeystrokeNavigator"/>
/// </summary>
public interface IListCollectionNavigator : ICollectionNavigator
{
    /// <summary>The collection of objects to search. <see cref="object.ToString()"/> is used to search the collection.</summary>
    IList Collection { get; set; }
}
