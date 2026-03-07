using Microsoft.AspNetCore.Mvc;
using SmartWash.Models;
using SmartWash.Services;
using static Postgrest.Constants;

namespace SmartWash.Controllers
{
    public class OrderController : Controller
    {
        private readonly Supabase.Client _supabase;
        private readonly IEmailService _emailService;

        public OrderController(Supabase.Client supabase, IEmailService emailService)
        {
            _supabase = supabase;
            _emailService = emailService;
        }

        [HttpGet]
        public async Task<IActionResult> Place()
        {
            var services = await _supabase.From<Service>().Get();
            ViewBag.Services = services.Models?.Where(s => s.IsActive).ToList() ?? new List<Service>();

            var detergents = await _supabase.From<Detergent>().Get();
            ViewBag.Detergents = detergents.Models?.Where(d => d.IsActive == true).ToList() ?? new List<Detergent>();

            ViewBag.UserName = HttpContext.Session.GetString("UserName") ?? "Guest";
            ViewBag.UserRole = HttpContext.Session.GetString("UserRole") ?? "Guest";
            ViewBag.UserEmail = HttpContext.Session.GetString("UserEmail") ?? "";
            ViewBag.UserPhone = HttpContext.Session.GetString("UserPhone") ?? "";
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Place(string serviceId, string detergentId, string pickupAddress,
            double pickupLat, double pickupLng, string deliveryAddress, double deliveryLat, double deliveryLng,
            string specialInstructions, string pickupDate)
        {
            var customerId = HttpContext.Session.GetString("UserId") ?? "";
            if (string.IsNullOrEmpty(customerId))
            {
                return Unauthorized("User not logged in.");
            }

            var service = await _supabase.From<Service>()
                .Filter("id", Operator.Equals, serviceId)
                .Single();

            if (service == null) return NotFound("Service not found.");

            var detergent = await _supabase.From<Detergent>()
                .Filter("id", Operator.Equals, detergentId)
                .Single();

            if (detergent == null) return NotFound("Detergent not found.");

            var pickupDt = DateTime.Parse(pickupDate);

            var order = new Order
            {
                Id = Guid.NewGuid().ToString(),
                CustomerId = customerId,
                ServiceId = serviceId,
                DetergentId = detergentId,
                PickupAddress = pickupAddress,
                PickupLat = pickupLat,
                PickupLng = pickupLng,
                DeliveryAddress = deliveryAddress,
                DeliveryLat = deliveryLat,
                DeliveryLng = deliveryLng,
                SpecialInstructions = specialInstructions,
                Status = "Pending",
                PickupDate = pickupDt
            };

            await _supabase.From<Order>().Insert(order);

            var customerEmail = HttpContext.Session.GetString("UserEmail") ?? "";
            var customerName = HttpContext.Session.GetString("UserName") ?? "Customer";
            await _emailService.SendStatusEmail(customerEmail, customerName, order.Id, service.Name, detergent.Name, null, null, "Pending");

            return RedirectToAction("Dashboard", "Customer");
        }

        [HttpGet]
        public async Task<IActionResult> Track(string id)
        {
            var order = await _supabase.From<Order>()
                .Filter("id", Operator.Equals, id)
                .Single();

            if (order == null) return NotFound();

            var service = await _supabase.From<Service>()
                .Filter("id", Operator.Equals, order.ServiceId ?? "")
                .Single();

            if (service == null) return NotFound("Service not found.");

            ViewBag.Order = order;
            ViewBag.Service = service;

            if (!string.IsNullOrEmpty(order.DetergentId))
            {
                var detergent = await _supabase.From<Detergent>()
                    .Filter("id", Operator.Equals, order.DetergentId)
                    .Single();
                if (detergent != null) ViewBag.Detergent = detergent;
            }

            if (!string.IsNullOrEmpty(order.PickupRiderId))
            {
                var pickupRider = await _supabase.From<Profile>()
                    .Filter("id", Operator.Equals, order.PickupRiderId)
                    .Single();
                if (pickupRider != null) ViewBag.PickupRider = pickupRider;
            }

            if (!string.IsNullOrEmpty(order.DeliveryRiderId))
            {
                var deliveryRider = await _supabase.From<Profile>()
                    .Filter("id", Operator.Equals, order.DeliveryRiderId)
                    .Single();
                if (deliveryRider != null) ViewBag.DeliveryRider = deliveryRider;
            }

            if (!string.IsNullOrEmpty(order.WarehouseId))
            {
                var warehouse = await _supabase.From<Warehouse>()
                    .Filter("id", Operator.Equals, order.WarehouseId)
                    .Single();
                if (warehouse != null) ViewBag.Warehouse = warehouse;
            }

            var customer = await _supabase.From<Profile>()
                .Filter("id", Operator.Equals, order.CustomerId ?? "")
                .Single();
            if (customer != null) ViewBag.Customer = customer;

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> History(string statusFilter)
        {
            var customerId = HttpContext.Session.GetString("UserId") ?? "";
            var query = _supabase.From<Order>()
                .Filter("customer_id", Operator.Equals, customerId);

            if (!string.IsNullOrEmpty(statusFilter) && statusFilter != "All")
            {
                query = query.Filter("status", Operator.Equals, statusFilter);
            }

            var ordersResponse = await query.Order("created_at", Ordering.Descending).Get();

            var services = await _supabase.From<Service>().Get();
            var serviceDict = services.Models?.ToDictionary(s => s.Id, s => s.Name) ?? new Dictionary<string, string>();

            var detergents = await _supabase.From<Detergent>().Get();
            var detergentDict = detergents.Models?.ToDictionary(d => d.Id, d => d.Name) ?? new Dictionary<string, string>();

            var reviewsResponse = await _supabase.From<Review>()
                .Filter("customer_id", Operator.Equals, customerId)
                .Get();
            var reviewedOrderIds = reviewsResponse.Models?.Select(r => r.OrderId).ToHashSet() ?? new HashSet<string>();

            ViewBag.Orders = ordersResponse.Models ?? new List<Order>();
            ViewBag.ServiceDict = serviceDict;
            ViewBag.DetergentDict = detergentDict;
            ViewBag.CurrentFilter = statusFilter ?? "All";
            ViewBag.ReviewedOrderIds = reviewedOrderIds;

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Receipt(string id)
        {
            var order = await _supabase.From<Order>()
                .Filter("id", Operator.Equals, id)
                .Single();

            if (order == null) return NotFound();

            var service = await _supabase.From<Service>()
                .Filter("id", Operator.Equals, order.ServiceId ?? "")
                .Single();

            if (service == null) return NotFound("Service not found.");

            var customer = await _supabase.From<Profile>()
                .Filter("id", Operator.Equals, order.CustomerId ?? "")
                .Single();

            if (customer == null) return NotFound("Customer profile not found.");

            ViewBag.Order = order;
            ViewBag.Service = service;
            ViewBag.Customer = customer;

            if (!string.IsNullOrEmpty(order.DetergentId))
            {
                var detergent = await _supabase.From<Detergent>()
                    .Filter("id", Operator.Equals, order.DetergentId)
                    .Single();
                if (detergent != null) ViewBag.Detergent = detergent;
            }

            if (!string.IsNullOrEmpty(order.WarehouseId))
            {
                var warehouse = await _supabase.From<Warehouse>()
                    .Filter("id", Operator.Equals, order.WarehouseId)
                    .Single();
                if (warehouse != null) ViewBag.Warehouse = warehouse;
            }

            return View();
        }
    }
}
