namespace Binnaculum.Extensions;

public class RxCollectionViewEvents(CollectionView data) 
    : RxReorderableItemsViewEvents(data)
{
    private readonly CollectionView _data = data;
}
