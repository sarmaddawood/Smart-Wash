using Postgrest.Attributes;
using Postgrest.Models;

namespace SmartWash.Models
{
    [Table("reviews")]
    public class Review : BaseModel
    {
        [PrimaryKey("id")]
        public string Id { get; set; } = string.Empty;

        [Column("order_id")]
        public string OrderId { get; set; } = string.Empty;

        [Column("customer_id")]
        public string CustomerId { get; set; } = string.Empty;

        [Column("rating")]
        public int Rating { get; set; }

        [Column("comment")]
        public string Comment { get; set; } = string.Empty;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
    }
}
