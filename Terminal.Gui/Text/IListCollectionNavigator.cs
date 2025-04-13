using System.Collections;

namespace Terminal.Gui;

public interface IListCollectionNavigator : ICollectionNavigator
{
    /// <summary>The collection of objects to search. <see cref="object.ToString()"/> is used to search the collection.</summary>
    IList Collection { get; set; }
}
