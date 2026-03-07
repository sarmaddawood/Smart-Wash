using Postgrest.Attributes;
using Postgrest.Models;

namespace SmartWash.Models
{
    [Table("roles")]
    public class Role : BaseModel
    {
        [PrimaryKey("id")]
        public string Id { get; set; } = string.Empty;

        [Column("name")]
        public string Name { get; set; } = string.Empty;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
    }
}
