using Postgrest.Attributes;
using Postgrest.Models;

namespace SmartWash.Models
{
    [Table("services")]
    public class Service : BaseModel
    {
        [PrimaryKey("id")]
        public long Id { get; set; }

        [Column("name")]
        public string Name { get; set; }

        [Column("description")]
        public string Description { get; set; }

        [Column("price_per_kg")]
        public double PricePerKg { get; set; }

        [Column("icon")]
        public string Icon { get; set; }

        [Column("max_kg")]
        public double MaxKg { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; }
    }
}
