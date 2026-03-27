using Postgrest.Attributes;
using Postgrest.Models;

namespace SmartWash.Models
{
    [Table("orders")]
    public class Order : BaseModel
    {
        [PrimaryKey("id")]
        public string Id { get; set; } = string.Empty;

        [Column("customer_id")]
        public string? CustomerId { get; set; }

        [Column("service_id")]
        public string? ServiceId { get; set; }

        [Column("staff_id")]
        public string? StaffId { get; set; }

        [Column("warehouse_id")]
        public string? WarehouseId { get; set; }

        [Column("weight_kg")]
        public decimal? WeightKg { get; set; }

        [Column("total_price")]
        public decimal? TotalPrice { get; set; }

        [Column("pickup_address")]
        public string? PickupAddress { get; set; }

        [Column("pickup_lat")]
        public double? PickupLat { get; set; }

        [Column("pickup_lng")]
        public double? PickupLng { get; set; }

        [Column("delivery_address")]
        public string? DeliveryAddress { get; set; }

        [Column("delivery_lat")]
        public double? DeliveryLat { get; set; }

        [Column("delivery_lng")]
        public double? DeliveryLng { get; set; }

        [Column("special_instructions")]
        public string? SpecialInstructions { get; set; }

        [Column("status")]
        public string Status { get; set; } = string.Empty;

        [Column("pickup_date")]
        public DateTime? PickupDate { get; set; }

        [Column("estimated_delivery")]
        public DateTime? EstimatedDelivery { get; set; }

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        [Column("detergent_id")]
        public string? DetergentId { get; set; }

        [Column("pickup_rider_id")]
        public string? PickupRiderId { get; set; }

        [Column("delivery_rider_id")]
        public string? DeliveryRiderId { get; set; }

        [Column("conditioner_id")]
        public string? ConditionerId { get; set; }
    }
}
