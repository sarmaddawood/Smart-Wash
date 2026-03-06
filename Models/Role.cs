using Postgrest.Attributes;
using Postgrest.Models;

namespace SmartWash.Models
{
    [Table("roles")]
    public class Role : BaseModel
    {
        [PrimaryKey("id")]
        public long Id { get; set; }

        [Column("name")]
        public string Name { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
    }
}
