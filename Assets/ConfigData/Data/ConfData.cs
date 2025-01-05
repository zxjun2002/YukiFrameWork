using MemoryPack;

[MemoryPackable]
public partial class ConfData
{
    public Item[] ItemCt;
}

[MemoryPackable]
public partial class Item
{
    public int ItemId;
    public string ItemName;
    public long ItemPId;
    public string ItemJson;
    public int[] ItemList;
    public string[] ItemDesList;
}
