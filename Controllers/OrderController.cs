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
            ViewBag.Services = services.Models.Where(s => s.IsActive).ToList();
            ViewBag.UserName = HttpContext.Session.GetString("UserName");
            ViewBag.UserEmail = HttpContext.Session.GetString("UserEmail");
            ViewBag.UserPhone = HttpContext.Session.GetString("UserPhone");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Place(long serviceId, double weightKg, string pickupAddress,
            double pickupLat, double pickupLng, string deliveryAddress, double deliveryLat, double deliveryLng,
            string specialInstructions, string pickupDate)
        {
            var customerId = HttpContext.Session.GetString("UserId");

            var service = await _supabase.From<Service>()
                .Filter("id", Operator.Equals, serviceId.ToString())
                .Single();

            var totalPrice = service.PricePerKg * weightKg;

            var isExpress = service.Name.Contains("Express");
            var pickupDt = DateTime.Parse(pickupDate);
            var estimatedDelivery = isExpress ? pickupDt.ToString("yyyy-MM-dd") : pickupDt.AddDays(1).ToString("yyyy-MM-dd");

            var order = new Order
            {
                CustomerId = customerId,
                ServiceId = serviceId,
                WeightKg = weightKg,
                TotalPrice = totalPrice,
                PickupAddress = pickupAddress,
                PickupLat = pickupLat,
                PickupLng = pickupLng,
                DeliveryAddress = deliveryAddress,
                DeliveryLat = deliveryLat,
                DeliveryLng = deliveryLng,
                SpecialInstructions = specialInstructions,
                Status = "Pending",
                PickupDate = pickupDate,
                EstimatedDelivery = estimatedDelivery
            };

            await _supabase.From<Order>().Insert(order);

            var customerEmail = HttpContext.Session.GetString("UserEmail");
            var customerName = HttpContext.Session.GetString("UserName");
            await _emailService.SendStatusEmail(customerEmail, customerName, order.Id, service.Name, weightKg, totalPrice, "Pending");

            return RedirectToAction("Dashboard", "Customer");
        }

        [HttpGet]
        public async Task<IActionResult> Track(long id)
        {
            var order = await _supabase.From<Order>()
                .Filter("id", Operator.Equals, id.ToString())
                .Single();

            var service = await _supabase.From<Service>()
                .Filter("id", Operator.Equals, order.ServiceId.ToString())
                .Single();

            ViewBag.Order = order;
            ViewBag.Service = service;

            if (!string.IsNullOrEmpty(order.RiderId))
            {
                var rider = await _supabase.From<Profile>()
                    .Filter("id", Operator.Equals, order.RiderId)
                    .Single();
                ViewBag.Rider = rider;
            }

            if (order.WarehouseId.HasValue)
            {
                var warehouse = await _supabase.From<Warehouse>()
                    .Filter("id", Operator.Equals, order.WarehouseId.Value.ToString())
                    .Single();
                ViewBag.Warehouse = warehouse;
            }

            var customer = await _supabase.From<Profile>()
                .Filter("id", Operator.Equals, order.CustomerId)
                .Single();
            ViewBag.Customer = customer;

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> History(string statusFilter)
        {
            var customerId = HttpContext.Session.GetString("UserId");
            var query = _supabase.From<Order>()
                .Filter("customer_id", Operator.Equals, customerId);

            if (!string.IsNullOrEmpty(statusFilter) && statusFilter != "All")
            {
                query = query.Filter("status", Operator.Equals, statusFilter);
            }

            var orders = await query.Order("created_at", Ordering.Descending).Get();

            var services = await _supabase.From<Service>().Get();
            var serviceDict = services.Models.ToDictionary(s => s.Id, s => s.Name);

            var reviews = await _supabase.From<Review>()
                .Filter("customer_id", Operator.Equals, customerId)
                .Get();
            var reviewedOrderIds = reviews.Models.Select(r => r.OrderId).ToHashSet();

            ViewBag.Orders = orders.Models;
            ViewBag.ServiceDict = serviceDict;
            ViewBag.CurrentFilter = statusFilter ?? "All";
            ViewBag.ReviewedOrderIds = reviewedOrderIds;

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Receipt(long id)
        {
            var order = await _supabase.From<Order>()
                .Filter("id", Operator.Equals, id.ToString())
                .Single();

            var service = await _supabase.From<Service>()
                .Filter("id", Operator.Equals, order.ServiceId.ToString())
                .Single();

            var customer = await _supabase.From<Profile>()
                .Filter("id", Operator.Equals, order.CustomerId)
                .Single();

            ViewBag.Order = order;
            ViewBag.Service = service;
            ViewBag.Customer = customer;

            if (order.WarehouseId.HasValue)
            {
                var warehouse = await _supabase.From<Warehouse>()
                    .Filter("id", Operator.Equals, order.WarehouseId.Value.ToString())
                    .Single();
                ViewBag.Warehouse = warehouse;
            }

            return View();
        }
    }
}
