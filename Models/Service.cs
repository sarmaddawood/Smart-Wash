using Postgrest.Attributes;
using Postgrest.Models;

namespace SmartWash.Models
{
    [Table("services")]
    public class Service : BaseModel
    {
        [PrimaryKey("id")]
        public string Id { get; set; } = string.Empty;

        [Column("name")]
        public string Name { get; set; } = string.Empty;

        [Column("description")]
        public string Description { get; set; } = string.Empty;

        [Column("price_per_kg")]
        public decimal PricePerKg { get; set; }

        [Column("icon")]
        public string Icon { get; set; } = string.Empty;

        [Column("max_kg")]
        public decimal MaxKg { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
    }
}
