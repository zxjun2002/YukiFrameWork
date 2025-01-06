using MemoryPack;

[MemoryPackable]
public partial class ConfData
{
    public Item[] ItemCt;
}

[MemoryPackable]
public partial class Item
{
    public int Id { get; set; }
    public string Itemname { get; set; }
    public long Itempid { get; set; }
    public string Itemjson { get; set; }
    public int[] Itemlist { get; set; }
    public string[] Itemdeslist { get; set; }
    
}
