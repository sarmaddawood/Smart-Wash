using Postgrest.Attributes;
using Postgrest.Models;

namespace SmartWash.Models
{
    [Table("warehouses")]
    public class Warehouse : BaseModel
    {
        [PrimaryKey("id")]
        public string Id { get; set; } = string.Empty;

        [Column("name")]
        public string Name { get; set; } = string.Empty;

        [Column("address")]
        public string Address { get; set; } = string.Empty;

        [Column("lat")]
        public double Lat { get; set; }

        [Column("lng")]
        public double Lng { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
    }
}
