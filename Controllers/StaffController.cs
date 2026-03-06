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
            var staffId = HttpContext.Session.GetString("UserId");
            ViewBag.UserName = HttpContext.Session.GetString("UserName");

            var orders = await _supabase.From<Order>()
                .Filter("staff_id", Operator.Equals, staffId)
                .Order("created_at", Ordering.Descending)
                .Get();

            var relevantOrders = orders.Models
                .Where(o => o.Status == "At Warehouse" || o.Status == "Washing" || o.Status == "Ready for Delivery")
                .ToList();

            var services = await _supabase.From<Service>().Get();
            var serviceDict = services.Models.ToDictionary(s => s.Id, s => s.Name);

            var profiles = await _supabase.From<Profile>().Get();
            var profileDict = profiles.Models.ToDictionary(p => p.Id, p => p);

            var warehouses = await _supabase.From<Warehouse>().Get();
            var warehouseDict = warehouses.Models.ToDictionary(w => w.Id, w => w);

            ViewBag.Orders = relevantOrders;
            ViewBag.ServiceDict = serviceDict;
            ViewBag.ProfileDict = profileDict;
            ViewBag.WarehouseDict = warehouseDict;

            return View();
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
