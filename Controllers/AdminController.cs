using Microsoft.AspNetCore.Mvc;
using SmartWash.Models;
using SmartWash.Services;
using static Postgrest.Constants;

namespace SmartWash.Controllers
{
    public class AdminController : Controller
    {
        private readonly Supabase.Client _supabase;
        private readonly IEmailService _emailService;

        public AdminController(Supabase.Client supabase, IEmailService emailService)
        {
            _supabase = supabase;
            _emailService = emailService;
        }

        public async Task<IActionResult> Dashboard()
        {
            ViewBag.UserName = HttpContext.Session.GetString("UserName");

            var orders = await _supabase.From<Order>()
                .Order("created_at", Ordering.Descending)
                .Get();

            var services = await _supabase.From<Service>().Get();
            var serviceDict = services.Models.ToDictionary(s => s.Id, s => s.Name);

            var profiles = await _supabase.From<Profile>().Get();
            var profileDict = profiles.Models.ToDictionary(p => p.Id, p => p);

            var roles = await _supabase.From<Role>().Get();
            var roleDict = roles.Models.ToDictionary(r => r.Id, r => r.Name);

            var warehouses = await _supabase.From<Warehouse>().Get();

            var staffRole = roles.Models.FirstOrDefault(r => r.Name == "staff");
            var riderRole = roles.Models.FirstOrDefault(r => r.Name == "rider");
            var customerRole = roles.Models.FirstOrDefault(r => r.Name == "customer");

            var staffList = staffRole != null ? profiles.Models.Where(p => p.RoleId == staffRole.Id).ToList() : new List<Profile>();
            var riderList = riderRole != null ? profiles.Models.Where(p => p.RoleId == riderRole.Id).ToList() : new List<Profile>();
            var customerList = customerRole != null ? profiles.Models.Where(p => p.RoleId == customerRole.Id).ToList() : new List<Profile>();

            var totalRevenue = orders.Models.Where(o => o.Status == "Delivered").Sum(o => o.TotalPrice);

            var announcements = await _supabase.From<Announcement>()
                .Order("created_at", Ordering.Descending)
                .Get();

            var reviews = await _supabase.From<Review>().Get();
            var reviewDict = reviews.Models.ToDictionary(r => r.OrderId, r => r);

            ViewBag.Orders = orders.Models;
            ViewBag.ServiceDict = serviceDict;
            ViewBag.ProfileDict = profileDict;
            ViewBag.RoleDict = roleDict;
            ViewBag.Warehouses = warehouses.Models;
            ViewBag.ActiveWarehouses = warehouses.Models.Where(w => w.IsActive).ToList();
            ViewBag.StaffList = staffList;
            ViewBag.RiderList = riderList;
            ViewBag.CustomerList = customerList;
            ViewBag.TotalRevenue = totalRevenue;
            ViewBag.Announcements = announcements.Models;
            ViewBag.ReviewDict = reviewDict;

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AssignStaff(long orderId, string staffId)
        {
            var order = await _supabase.From<Order>()
                .Filter("id", Operator.Equals, orderId.ToString())
                .Single();

            order.StaffId = staffId;
            await _supabase.From<Order>().Update(order);

            return RedirectToAction("Dashboard");
        }

        [HttpPost]
        public async Task<IActionResult> AssignWarehouse(long orderId, long warehouseId)
        {
            var order = await _supabase.From<Order>()
                .Filter("id", Operator.Equals, orderId.ToString())
                .Single();

            order.WarehouseId = warehouseId;
            await _supabase.From<Order>().Update(order);

            return RedirectToAction("Dashboard");
        }

        [HttpPost]
        public async Task<IActionResult> UpdateStatus(long orderId, string status)
        {
            var order = await _supabase.From<Order>()
                .Filter("id", Operator.Equals, orderId.ToString())
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

        [HttpPost]
        public async Task<IActionResult> CreateAnnouncement(string title, string message, bool isActive)
        {
            var announcement = new Announcement
            {
                Title = title,
                Message = message,
                IsActive = isActive
            };

            await _supabase.From<Announcement>().Insert(announcement);
            return RedirectToAction("Dashboard");
        }

        [HttpPost]
        public async Task<IActionResult> UpdateAnnouncement(long id, string title, string message, bool isActive)
        {
            var announcement = await _supabase.From<Announcement>()
                .Filter("id", Operator.Equals, id.ToString())
                .Single();

            announcement.Title = title;
            announcement.Message = message;
            announcement.IsActive = isActive;
            await _supabase.From<Announcement>().Update(announcement);

            return RedirectToAction("Dashboard");
        }

        [HttpGet]
        public async Task<IActionResult> Services()
        {
            ViewBag.UserName = HttpContext.Session.GetString("UserName");
            var services = await _supabase.From<Service>().Get();
            ViewBag.Services = services.Models;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateService(string name, string description, double pricePerKg, string icon, double maxKg, bool isActive)
        {
            var service = new Service
            {
                Name = name,
                Description = description,
                PricePerKg = pricePerKg,
                Icon = icon,
                MaxKg = maxKg,
                IsActive = isActive
            };

            await _supabase.From<Service>().Insert(service);
            return RedirectToAction("Services");
        }

        [HttpPost]
        public async Task<IActionResult> UpdateService(long id, string name, string description, double pricePerKg, string icon, double maxKg, bool isActive)
        {
            var service = await _supabase.From<Service>()
                .Filter("id", Operator.Equals, id.ToString())
                .Single();

            service.Name = name;
            service.Description = description;
            service.PricePerKg = pricePerKg;
            service.Icon = icon;
            service.MaxKg = maxKg;
            service.IsActive = isActive;
            await _supabase.From<Service>().Update(service);

            return RedirectToAction("Services");
        }

        [HttpGet]
        public async Task<IActionResult> Warehouses()
        {
            ViewBag.UserName = HttpContext.Session.GetString("UserName");
            var warehouses = await _supabase.From<Warehouse>().Get();
            ViewBag.Warehouses = warehouses.Models;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateWarehouse(string name, string address, double lat, double lng, bool isActive)
        {
            var warehouse = new Warehouse
            {
                Name = name,
                Address = address,
                Lat = lat,
                Lng = lng,
                IsActive = isActive
            };

            await _supabase.From<Warehouse>().Insert(warehouse);
            return RedirectToAction("Warehouses");
        }

        [HttpPost]
        public async Task<IActionResult> UpdateWarehouse(long id, string name, string address, double lat, double lng, bool isActive)
        {
            var warehouse = await _supabase.From<Warehouse>()
                .Filter("id", Operator.Equals, id.ToString())
                .Single();

            warehouse.Name = name;
            warehouse.Address = address;
            warehouse.Lat = lat;
            warehouse.Lng = lng;
            warehouse.IsActive = isActive;
            await _supabase.From<Warehouse>().Update(warehouse);

            return RedirectToAction("Warehouses");
        }

        [HttpGet]
        public async Task<IActionResult> CreateAccount()
        {
            ViewBag.UserName = HttpContext.Session.GetString("UserName");
            var roles = await _supabase.From<Role>().Get();
            ViewBag.Roles = roles.Models.Where(r => r.Name == "staff" || r.Name == "rider").ToList();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateAccount(string fullName, string email, string password, string phone, long roleId)
        {
            var session = await _supabase.Auth.SignUp(email, password);

            var profile = new Profile
            {
                Id = session.User.Id,
                FullName = fullName,
                Email = email,
                Phone = phone,
                RoleId = roleId
            };

            await _supabase.From<Profile>().Insert(profile);

            return RedirectToAction("Dashboard");
        }
    }
}
