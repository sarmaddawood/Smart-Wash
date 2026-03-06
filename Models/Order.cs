using Postgrest.Attributes;
using Postgrest.Models;

namespace SmartWash.Models
{
    [Table("orders")]
    public class Order : BaseModel
    {
        [PrimaryKey("id")]
        public long Id { get; set; }

        [Column("customer_id")]
        public string CustomerId { get; set; }

        [Column("service_id")]
        public long ServiceId { get; set; }

        [Column("staff_id")]
        public string StaffId { get; set; }

        [Column("rider_id")]
        public string RiderId { get; set; }

        [Column("warehouse_id")]
        public long? WarehouseId { get; set; }

        [Column("weight_kg")]
        public double WeightKg { get; set; }

        [Column("total_price")]
        public double TotalPrice { get; set; }

        [Column("pickup_address")]
        public string PickupAddress { get; set; }

        [Column("pickup_lat")]
        public double PickupLat { get; set; }

        [Column("pickup_lng")]
        public double PickupLng { get; set; }

        [Column("delivery_address")]
        public string DeliveryAddress { get; set; }

        [Column("delivery_lat")]
        public double DeliveryLat { get; set; }

        [Column("delivery_lng")]
        public double DeliveryLng { get; set; }

        [Column("special_instructions")]
        public string SpecialInstructions { get; set; }

        [Column("status")]
        public string Status { get; set; }

        [Column("pickup_date")]
        public string PickupDate { get; set; }

        [Column("estimated_delivery")]
        public string EstimatedDelivery { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
    }
}
