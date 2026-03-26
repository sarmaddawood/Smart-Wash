using Postgrest.Attributes;
using Postgrest.Models;

namespace SmartWash.Models
{
    [Table("profiles")]
    public class Profile : BaseModel
    {
        [PrimaryKey("id")]
        public string Id { get; set; } = string.Empty;

        [Column("full_name")]
        public string FullName { get; set; } = string.Empty;

        [Column("email")]
        public string Email { get; set; } = string.Empty;

        [Column("phone")]
        public string Phone { get; set; } = string.Empty;

        [Column("role_id")]
        public string RoleId { get; set; } = string.Empty;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("password")]
        public string Password { get; set; } = string.Empty;
    }
}