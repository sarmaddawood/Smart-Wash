using Microsoft.AspNetCore.Mvc;
using SmartWash.Models;
using SmartWash.Services;
using static Postgrest.Constants;

namespace SmartWash.Controllers
{
    public class StaffController : Controller
    {
        private readonly Supabase.Client _supabase;
        private readonly IEmailService _emailService;

        public StaffController(Supabase.Client supabase, IEmailService emailService)
        {
            _supabase = supabase;
            _emailService = emailService;
        }

        public async Task<IActionResult> Dashboard()
        {
            ViewBag.UserName = HttpContext.Session.GetString("UserName") ?? "Staff";

            var staffId = HttpContext.Session.GetString("UserId");

            var allOrdersResponse = await _supabase.From<Order>()
                .Order("created_at", Ordering.Descending)
                .Get();

            var availableOrders = allOrdersResponse.Models?
                .Where(o => o.Status == "At Warehouse" && string.IsNullOrEmpty(o.StaffId))
                .ToList() ?? new List<Order>();

            var myActiveOrders = allOrdersResponse.Models?
                .Where(o => o.StaffId == staffId && o.Status != "Delivered" && o.Status != "Cancelled")
                .ToList() ?? new List<Order>();

            var servicesResponse = await _supabase.From<Service>().Get();
            var serviceDict = servicesResponse.Models?.ToDictionary(s => s.Id, s => s.Name) ?? new Dictionary<string, string>();
            var servicePriceDict = servicesResponse.Models?.ToDictionary(s => s.Id, s => s.PricePerKg) ?? new Dictionary<string, decimal>();

            var detergentsResponse = await _supabase.From<Detergent>().Get();
            var detergentDict = detergentsResponse.Models?.ToDictionary(d => d.Id, d => d.Name) ?? new Dictionary<string, string>();

            var conditionersResponse = await _supabase.From<Conditioner>().Get();
            var conditionerDict = conditionersResponse.Models?.ToDictionary(c => c.Id, c => c.Name) ?? new Dictionary<string, string>();

            var profilesResponse = await _supabase.From<Profile>().Get();
            var profileDict = profilesResponse.Models?.ToDictionary(p => p.Id, p => p) ?? new Dictionary<string, Profile>();

            var warehousesResponse = await _supabase.From<Warehouse>().Get();
            var warehouseDict = warehousesResponse.Models?.ToDictionary(w => w.Id, w => w) ?? new Dictionary<string, Warehouse>();

            ViewBag.AvailableOrders = availableOrders;
            ViewBag.MyActiveOrders = myActiveOrders;
            ViewBag.ServiceDict = serviceDict;
            ViewBag.ServicePriceDict = servicePriceDict;
            ViewBag.DetergentDict = detergentDict;
            ViewBag.ConditionerDict = conditionerDict;
            ViewBag.ProfileDict = profileDict;
            ViewBag.WarehouseDict = warehouseDict;

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

            if (!string.IsNullOrEmpty(order.ConditionerId))
            {
                var conditioner = await _supabase.From<Conditioner>().Filter("id", Operator.Equals, order.ConditionerId).Single();
                ViewBag.Conditioner = conditioner;
            }

            ViewBag.StaffId = HttpContext.Session.GetString("UserId") ?? "";

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AcceptOrder(string id)
        {
            var staffId = HttpContext.Session.GetString("UserId");
            var order = await _supabase.From<Order>().Filter("id", Operator.Equals, id).Single();

            if (order == null) return NotFound();
            if (!string.IsNullOrEmpty(order.StaffId)) return BadRequest("Order already taken.");

            order.StaffId = staffId;
            await _supabase.From<Order>().Update(order);

            return RedirectToAction("OrderDetail", new { id });
        }

        [HttpPost]
        public async Task<IActionResult> ConfirmWeight(string id, decimal weightKg)
        {
            var order = await _supabase.From<Order>()
                .Filter("id", Operator.Equals, id)
                .Single();

            if (order == null) return NotFound();

            var service = await _supabase.From<Service>()
                .Filter("id", Operator.Equals, order.ServiceId ?? "")
                .Single();

            if (service == null) return NotFound("Service not found.");

            order.WeightKg = weightKg;
            order.TotalPrice = weightKg * service.PricePerKg;
            order.Status = "Weighed & Measured";
            await _supabase.From<Order>().Update(order);

            var customer = await _supabase.From<Profile>()
                .Filter("id", Operator.Equals, order.CustomerId ?? "")
                .Single();

            if (customer == null) return NotFound("Customer profile not found.");

            var detergentName = "—";
            if (!string.IsNullOrEmpty(order.DetergentId))
            {
                var detergent = await _supabase.From<Detergent>()
                    .Filter("id", Operator.Equals, order.DetergentId)
                    .Single();
                if (detergent != null) detergentName = detergent.Name;
            }

            await _emailService.SendStatusEmail(customer.Email, customer.FullName, order.Id, service.Name, detergentName, order.WeightKg, order.TotalPrice, "Weighed & Measured");

            return RedirectToAction("Dashboard");
        }

        [HttpPost]
        public async Task<IActionResult> UpdateStatus(string id, string status)
        {
            var order = await _supabase.From<Order>()
                .Filter("id", Operator.Equals, id)
                .Single();

            if (order == null) return NotFound();

            order.Status = status;
            await _supabase.From<Order>().Update(order);

            var customer = await _supabase.From<Profile>()
                .Filter("id", Operator.Equals, order.CustomerId ?? "")
                .Single();

            if (customer == null) return NotFound("Customer profile not found.");

            var service = await _supabase.From<Service>()
                .Filter("id", Operator.Equals, order.ServiceId ?? "")
                .Single();

            if (service == null) return NotFound("Service not found.");
            var serviceRecord = await _supabase.From<Service>()
                .Filter("id", Operator.Equals, order.ServiceId ?? "")
                .Single();

            var detergentName = "—";
            if (!string.IsNullOrEmpty(order.DetergentId))
            {
                var detergent = await _supabase.From<Detergent>()
                    .Filter("id", Operator.Equals, order.DetergentId)
                    .Single();
                if (detergent != null) detergentName = detergent.Name;
            }

            await _emailService.SendStatusEmail(customer?.Email ?? "", customer?.FullName ?? "", order.Id, serviceRecord?.Name ?? "", detergentName, order.WeightKg, order.TotalPrice, status);

            return RedirectToAction("OrderDetail", new { id });
        }
    }
}
