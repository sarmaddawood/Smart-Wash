using Postgrest.Attributes;
using Postgrest.Models;

namespace SmartWash.Models
{
    [Table("detergents")]
    public class Detergent : BaseModel
    {
        [PrimaryKey("id", true)]
        public string Id { get; set; } = string.Empty;

        [Column("name")]
        public string Name { get; set; } = string.Empty;

        [Column("brand")]
        public string? Brand { get; set; }

        [Column("is_active")]
        public bool? IsActive { get; set; }

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }
    }
}
