using Microsoft.AspNetCore.Mvc;
using SmartWash.Models;
using SmartWash.Services;
using static Postgrest.Constants;

namespace SmartWash.Controllers
{
    public class RiderController : Controller
    {
        private readonly Supabase.Client _supabase;
        private readonly IEmailService _emailService;

        public RiderController(Supabase.Client supabase, IEmailService emailService)
        {
            _supabase = supabase;
            _emailService = emailService;
        }

        public async Task<IActionResult> Dashboard()
        {
            var riderId = HttpContext.Session.GetString("UserId") ?? "";
            ViewBag.UserName = HttpContext.Session.GetString("UserName") ?? "Rider";

            var pendingOrders = await _supabase.From<Order>().Filter("status", Operator.Equals, "Pending").Get();
            var readyOrders = await _supabase.From<Order>().Filter("status", Operator.Equals, "Ready for Delivery").Get();

            var allOrdersResponse = await _supabase.From<Order>().Order("created_at", Ordering.Descending).Get();
            var myActiveOrders = allOrdersResponse.Models
                .Where(o => (o.PickupRiderId == riderId || o.DeliveryRiderId == riderId)
                    && o.Status != "Delivered" && o.Status != "Cancelled")
                .ToList();

            var services = await _supabase.From<Service>().Get();
            var serviceDict = services.Models?.ToDictionary(s => s.Id, s => s.Name) ?? new Dictionary<string, string>();

            var profiles = await _supabase.From<Profile>().Get();
            var profileDict = profiles.Models?.ToDictionary(p => p.Id, p => p) ?? new Dictionary<string, Profile>();

            ViewBag.PendingOrders = pendingOrders.Models ?? new List<Order>();
            ViewBag.ReadyOrders = readyOrders.Models ?? new List<Order>();
            ViewBag.MyActiveOrders = myActiveOrders;
            ViewBag.ServiceDict = serviceDict;
            ViewBag.ProfileDict = profileDict;
            ViewBag.RiderId = riderId;

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> OrderDetail(string id)
        {
            var order = await _supabase.From<Order>().Filter("id", Operator.Equals, id).Single();
            if (order == null) return NotFound();

            var customer = await _supabase.From<Profile>().Filter("id", Operator.Equals, order.CustomerId ?? "").Single();
            var service = await _supabase.From<Service>().Filter("id", Operator.Equals, order.ServiceId ?? "").Single();
            
            ViewBag.Order = order;
            ViewBag.Customer = customer;
            ViewBag.Service = service;

            if (!string.IsNullOrEmpty(order.DetergentId))
            {
                var detergent = await _supabase.From<Detergent>().Filter("id", Operator.Equals, order.DetergentId).Single();
                ViewBag.Detergent = detergent;
            }

            if (!string.IsNullOrEmpty(order.WarehouseId))
            {
                var warehouse = await _supabase.From<Warehouse>().Filter("id", Operator.Equals, order.WarehouseId).Single();
                ViewBag.Warehouse = warehouse;
            }

            var warehouses = await _supabase.From<Warehouse>().Filter("is_active", Operator.Equals, "true").Get();
            ViewBag.Warehouses = warehouses.Models ?? new List<Warehouse>();

            ViewBag.RiderId = HttpContext.Session.GetString("UserId") ?? "";
            
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AcceptPickup(string id)
        {
            var riderId = HttpContext.Session.GetString("UserId");
            var order = await _supabase.From<Order>().Filter("id", Operator.Equals, id).Single();
            if (order == null) return NotFound();

            order.PickupRiderId = riderId;
            order.Status = "Picked Up";
            await _supabase.From<Order>().Update(order);

            var customer = await _supabase.From<Profile>().Filter("id", Operator.Equals, order.CustomerId ?? "").Single();
            var service = await _supabase.From<Service>().Filter("id", Operator.Equals, order.ServiceId ?? "").Single();

            var detergentName = "—";
            if (!string.IsNullOrEmpty(order.DetergentId))
            {
                var detergent = await _supabase.From<Detergent>().Filter("id", Operator.Equals, order.DetergentId).Single();
                detergentName = detergent?.Name ?? "—";
            }

            await _emailService.SendStatusEmail(customer?.Email ?? "", customer?.FullName ?? "", order.Id, service?.Name ?? "", detergentName, order.WeightKg, order.TotalPrice, "Picked Up");
            return RedirectToAction("OrderDetail", new { id });
        }

        [HttpPost]
        public async Task<IActionResult> MarkAtWarehouse(string id, string warehouseId)
        {
            var order = await _supabase.From<Order>().Filter("id", Operator.Equals, id).Single();
            if (order == null) return NotFound();

            order.Status = "At Warehouse";
            order.WarehouseId = warehouseId;
            await _supabase.From<Order>().Update(order);

            var customer = await _supabase.From<Profile>().Filter("id", Operator.Equals, order.CustomerId ?? "").Single();
            var service = await _supabase.From<Service>().Filter("id", Operator.Equals, order.ServiceId ?? "").Single();

            var detergentName = "—";
            if (!string.IsNullOrEmpty(order.DetergentId))
            {
                var detergent = await _supabase.From<Detergent>().Filter("id", Operator.Equals, order.DetergentId).Single();
                detergentName = detergent?.Name ?? "—";
            }

            await _emailService.SendStatusEmail(customer?.Email ?? "", customer?.FullName ?? "", order.Id, service?.Name ?? "", detergentName, order.WeightKg, order.TotalPrice, "At Warehouse");
            return RedirectToAction("OrderDetail", new { id });
        }

        [HttpPost]
        public async Task<IActionResult> AcceptDelivery(string id)
        {
            var riderId = HttpContext.Session.GetString("UserId") ?? "";
            var order = await _supabase.From<Order>().Filter("id", Operator.Equals, id).Single();
            if (order == null) return NotFound();

            order.DeliveryRiderId = riderId;
            order.Status = "Out for Delivery";
            await _supabase.From<Order>().Update(order);

            var customer = await _supabase.From<Profile>().Filter("id", Operator.Equals, order.CustomerId ?? "").Single();
            var service = await _supabase.From<Service>().Filter("id", Operator.Equals, order.ServiceId ?? "").Single();

            var detergentName = "—";
            if (!string.IsNullOrEmpty(order.DetergentId))
            {
                var detergent = await _supabase.From<Detergent>().Filter("id", Operator.Equals, order.DetergentId).Single();
                detergentName = detergent?.Name ?? "—";
            }

            await _emailService.SendStatusEmail(customer?.Email ?? "", customer?.FullName ?? "", order.Id, service?.Name ?? "", detergentName, order.WeightKg, order.TotalPrice, "Out for Delivery");
            return RedirectToAction("OrderDetail", new { id });
        }

        [HttpPost]
        public async Task<IActionResult> MarkDelivered(string id)
        {
            var order = await _supabase.From<Order>().Filter("id", Operator.Equals, id).Single();
            if (order == null) return NotFound();

            order.Status = "Delivered";
            await _supabase.From<Order>().Update(order);

            var customer = await _supabase.From<Profile>().Filter("id", Operator.Equals, order.CustomerId ?? "").Single();
            var service = await _supabase.From<Service>().Filter("id", Operator.Equals, order.ServiceId ?? "").Single();

            var detergentName = "—";
            if (!string.IsNullOrEmpty(order.DetergentId))
            {
                var detergent = await _supabase.From<Detergent>().Filter("id", Operator.Equals, order.DetergentId).Single();
                detergentName = detergent?.Name ?? "—";
            }

            await _emailService.SendStatusEmail(customer?.Email ?? "", customer?.FullName ?? "", order.Id, service?.Name ?? "", detergentName, order.WeightKg, order.TotalPrice, "Delivered");
            return RedirectToAction("OrderDetail", new { id });
        }
    }
}
