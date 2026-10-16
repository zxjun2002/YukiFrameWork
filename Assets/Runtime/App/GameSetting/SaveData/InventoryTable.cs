using SQLite;

namespace Domain
{
    [Table("InventoryTable")]
    public sealed class InventoryTable
    {
        [PrimaryKey] public int Id { get; set; }
        [Indexed] public int PlayerId { get; set; }
        [Indexed] public int ItemId { get; set; }
        public string Name { get; set; }
        public int Count { get; set; }
    }
}
