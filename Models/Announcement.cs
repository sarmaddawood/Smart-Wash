using Postgrest.Attributes;
using Postgrest.Models;

namespace SmartWash.Models
{
    [Table("announcements")]
    public class Announcement : BaseModel
    {
        [PrimaryKey("id")]
        public long Id { get; set; }

        [Column("title")]
        public string Title { get; set; }

        [Column("message")]
        public string Message { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
    }
}
