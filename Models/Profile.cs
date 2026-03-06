using Postgrest.Attributes;
using Postgrest.Models;

namespace SmartWash.Models
{
    [Table("profiles")]
    public class Profile : BaseModel
    {
        [PrimaryKey("id", false)]
        public string Id { get; set; }

        [Column("full_name")]
        public string FullName { get; set; }

        [Column("email")]
        public string Email { get; set; }

        [Column("phone")]
        public string Phone { get; set; }

        [Column("role_id")]
        public long RoleId { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
    }
}
