using Postgrest.Attributes;
using Postgrest.Models;

namespace SmartWash.Models
{
    [Table("conditioners")]
    public class Conditioner : BaseModel
    {
        [PrimaryKey("id", false)]
        public string Id { get; set; } = string.Empty;

        [Column("name")]
        public string Name { get; set; } = string.Empty;

        [Column("brand")]
        public string? Brand { get; set; }

        [Column("is_active")]
        public bool? IsActive { get; set; } = true;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
    }
}
