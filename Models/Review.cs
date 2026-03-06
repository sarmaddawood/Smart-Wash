using Postgrest.Attributes;
using Postgrest.Models;

namespace SmartWash.Models
{
    [Table("reviews")]
    public class Review : BaseModel
    {
        [PrimaryKey("id")]
        public long Id { get; set; }

        [Column("order_id")]
        public long OrderId { get; set; }

        [Column("customer_id")]
        public string CustomerId { get; set; }

        [Column("rating")]
        public int Rating { get; set; }

        [Column("comment")]
        public string Comment { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
    }
}
