using SQLite;

namespace Domain
{
    [Table("Player")]
    public sealed class Player
    {
        [PrimaryKey] public int Id { get; set; }
        public string Name { get; set; }
        public int Level { get; set; }
        public long Gold { get; set; }
    }
}
