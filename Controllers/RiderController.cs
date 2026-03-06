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
            var riderId = HttpContext.Session.GetString("UserId");
            ViewBag.UserName = HttpContext.Session.GetString("UserName");

            var pendingOrders = await _supabase.From<Order>()
                .Filter("status", Operator.Equals, "Pending")
                .Get();

            var readyOrders = await _supabase.From<Order>()
                .Filter("status", Operator.Equals, "Ready for Delivery")
                .Get();

            var myOrders = await _supabase.From<Order>()
                .Filter("rider_id", Operator.Equals, riderId)
                .Order("created_at", Ordering.Descending)
                .Get();

            var activeStatuses = new[] { "Rider Assigned", "Picked Up", "Out for Delivery" };
            var myActiveOrders = myOrders.Models.Where(o => activeStatuses.Contains(o.Status)).ToList();

            var services = await _supabase.From<Service>().Get();
            var serviceDict = services.Models.ToDictionary(s => s.Id, s => s.Name);

            var warehouses = await _supabase.From<Warehouse>().Get();
            var warehouseDict = warehouses.Models.ToDictionary(w => w.Id, w => w);

            var customerIds = myActiveOrders.Select(o => o.CustomerId)
                .Union(pendingOrders.Models.Select(o => o.CustomerId))
                .Union(readyOrders.Models.Select(o => o.CustomerId))
                .Distinct().ToList();

            var profiles = await _supabase.From<Profile>().Get();
            var profileDict = profiles.Models.ToDictionary(p => p.Id, p => p);

            ViewBag.PendingOrders = pendingOrders.Models;
            ViewBag.ReadyOrders = readyOrders.Models;
            ViewBag.MyActiveOrders = myActiveOrders;
            ViewBag.ServiceDict = serviceDict;
            ViewBag.WarehouseDict = warehouseDict;
            ViewBag.ProfileDict = profileDict;

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AcceptPickup(long id)
        {
            var riderId = HttpContext.Session.GetString("UserId");

            var order = await _supabase.From<Order>()
                .Filter("id", Operator.Equals, id.ToString())
                .Single();

            order.RiderId = riderId;
            order.Status = "Rider Assigned";
            await _supabase.From<Order>().Update(order);

            var customer = await _supabase.From<Profile>()
                .Filter("id", Operator.Equals, order.CustomerId)
                .Single();

            var service = await _supabase.From<Service>()
                .Filter("id", Operator.Equals, order.ServiceId.ToString())
                .Single();

            await _emailService.SendStatusEmail(customer.Email, customer.FullName, order.Id, service.Name, order.WeightKg, order.TotalPrice, "Rider Assigned");

            return RedirectToAction("Dashboard");
        }

        [HttpPost]
        public async Task<IActionResult> AcceptDelivery(long id)
        {
            var riderId = HttpContext.Session.GetString("UserId");

            var order = await _supabase.From<Order>()
                .Filter("id", Operator.Equals, id.ToString())
                .Single();

            order.RiderId = riderId;
            order.Status = "Out for Delivery";
            await _supabase.From<Order>().Update(order);

            var customer = await _supabase.From<Profile>()
                .Filter("id", Operator.Equals, order.CustomerId)
                .Single();

            var service = await _supabase.From<Service>()
                .Filter("id", Operator.Equals, order.ServiceId.ToString())
                .Single();

            await _emailService.SendStatusEmail(customer.Email, customer.FullName, order.Id, service.Name, order.WeightKg, order.TotalPrice, "Out for Delivery");

            return RedirectToAction("Dashboard");
        }

        [HttpPost]
        public async Task<IActionResult> UpdateStatus(long id, string status)
        {
            var order = await _supabase.From<Order>()
                .Filter("id", Operator.Equals, id.ToString())
                .Single();

            order.Status = status;
            await _supabase.From<Order>().Update(order);

            var customer = await _supabase.From<Profile>()
                .Filter("id", Operator.Equals, order.CustomerId)
                .Single();

            var service = await _supabase.From<Service>()
                .Filter("id", Operator.Equals, order.ServiceId.ToString())
                .Single();

            await _emailService.SendStatusEmail(customer.Email, customer.FullName, order.Id, service.Name, order.WeightKg, order.TotalPrice, status);

            return RedirectToAction("Dashboard");
        }
    }
}
