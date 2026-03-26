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
            ViewBag.UserName = HttpContext.Session.GetString("UserName") ?? "Admin";

            var ordersResponse = await _supabase.From<Order>()
                .Order("created_at", Ordering.Descending)
                .Get();

            var servicesResponse = await _supabase.From<Service>().Get();
            var serviceDict = servicesResponse.Models?.ToDictionary(s => s.Id, s => s.Name) ?? new Dictionary<string, string>();

            var detergentsResponse = await _supabase.From<Detergent>().Get();
            var detergentDict = detergentsResponse.Models?.ToDictionary(d => d.Id, d => d.Name) ?? new Dictionary<string, string>();

            var profilesResponse = await _supabase.From<Profile>().Get();
            var profileDict = profilesResponse.Models?.ToDictionary(p => p.Id, p => p) ?? new Dictionary<string, Profile>();

            var rolesResponse = await _supabase.From<Role>().Get();
            var roles = rolesResponse.Models ?? new List<Role>();
            var roleDict = roles.ToDictionary(r => r.Id, r => r.Name);

            var warehousesResponse = await _supabase.From<Warehouse>().Get();
            var warehouses = warehousesResponse.Models ?? new List<Warehouse>();

            var staffRole = roles.FirstOrDefault(r => r.Name == "staff");
            var riderRole = roles.FirstOrDefault(r => r.Name == "rider");
            var customerRole = roles.FirstOrDefault(r => r.Name == "customer");

            var profiles = profilesResponse.Models ?? new List<Profile>();
            var staffList = staffRole != null ? profiles.Where(p => p.RoleId == staffRole.Id).ToList() : new List<Profile>();
            var riderList = riderRole != null ? profiles.Where(p => p.RoleId == riderRole.Id).ToList() : new List<Profile>();
            var customerList = customerRole != null ? profiles.Where(p => p.RoleId == customerRole.Id).ToList() : new List<Profile>();

            var orders = ordersResponse.Models ?? new List<Order>();
            var totalRevenue = orders.Where(o => o.Status == "Delivered").Sum(o => o.TotalPrice ?? 0m);

            var announcementsResponse = await _supabase.From<Announcement>()
                .Order("created_at", Ordering.Descending)
                .Get();

            var reviewsResponse = await _supabase.From<Review>().Get();
            var reviewDict = reviewsResponse.Models?.ToDictionary(r => r.OrderId, r => r) ?? new Dictionary<string, Review>();

            ViewBag.Orders = orders;
            ViewBag.ServiceDict = serviceDict;
            ViewBag.Services = servicesResponse.Models ?? new List<Service>();
            ViewBag.DetergentDict = detergentDict;
            ViewBag.Detergents = detergentsResponse.Models ?? new List<Detergent>();
            ViewBag.ProfileDict = profileDict;
            ViewBag.RoleDict = roleDict;
            ViewBag.Warehouses = warehouses;
            ViewBag.ActiveWarehouses = warehouses.Where(w => w.IsActive).ToList();
            ViewBag.StaffList = staffList;
            ViewBag.RiderList = riderList;
            ViewBag.CustomerList = customerList;
            ViewBag.TotalRevenue = totalRevenue;
            ViewBag.Announcements = announcementsResponse.Models ?? new List<Announcement>();
            ViewBag.ReviewDict = reviewDict;

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AssignStaff(string orderId, string staffId)
        {
            var order = await _supabase.From<Order>()
                .Filter("id", Operator.Equals, orderId)
                .Single();

            if (order == null) return NotFound();

            order.StaffId = staffId;
            await _supabase.From<Order>().Update(order);

            return RedirectToAction("Dashboard");
        }

        [HttpPost]
        public async Task<IActionResult> AssignWarehouse(string orderId, string warehouseId)
        {
            var order = await _supabase.From<Order>()
                .Filter("id", Operator.Equals, orderId)
                .Single();

            if (order == null) return NotFound();

            order.WarehouseId = warehouseId;
            await _supabase.From<Order>().Update(order);

            return RedirectToAction("Dashboard");
        }

        [HttpPost]
        public async Task<IActionResult> UpdateStatus(string orderId, string status)
        {
            var order = await _supabase.From<Order>()
                .Filter("id", Operator.Equals, orderId)
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

            var detergentName = "—";
            if (!string.IsNullOrEmpty(order.DetergentId))
            {
                var detergent = await _supabase.From<Detergent>()
                    .Filter("id", Operator.Equals, order.DetergentId)
                    .Single();
                if (detergent != null) detergentName = detergent.Name;
            }

            await _emailService.SendStatusEmail(customer.Email, customer.FullName, order.Id, service.Name, detergentName, order.WeightKg, order.TotalPrice, status);

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
        public async Task<IActionResult> UpdateAnnouncement(string id, string title, string message, bool isActive)
        {
            var announcement = await _supabase.From<Announcement>()
                .Filter("id", Operator.Equals, id)
                .Single();

            if (announcement == null) return NotFound();

            announcement.Title = title;
            announcement.Message = message;
            announcement.IsActive = isActive;
            await _supabase.From<Announcement>().Update(announcement);

            return RedirectToAction("Dashboard");
        }

        [HttpGet]
        public async Task<IActionResult> Services()
        {
            ViewBag.UserName = HttpContext.Session.GetString("UserName") ?? "Admin";
            var servicesResponse = await _supabase.From<Service>().Get();
            ViewBag.Services = servicesResponse.Models ?? new List<Service>();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateService(string name, string description, decimal pricePerKg, string icon, decimal maxKg, bool isActive)
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
            return RedirectToAction("Dashboard");
        }

        [HttpPost]
        public async Task<IActionResult> UpdateService(string id, string name, string description, decimal pricePerKg, string icon, decimal maxKg, bool isActive)
        {
            var service = await _supabase.From<Service>()
                .Filter("id", Operator.Equals, id)
                .Single();

            if (service == null) return NotFound();

            service.Name = name;
            service.Description = description;
            service.PricePerKg = pricePerKg;
            service.Icon = icon;
            service.MaxKg = maxKg;
            service.IsActive = isActive;
            await _supabase.From<Service>().Update(service);

            return RedirectToAction("Dashboard");
        }

        [HttpGet]
        public async Task<IActionResult> Warehouses()
        {
            ViewBag.UserName = HttpContext.Session.GetString("UserName") ?? "Admin";
            var warehousesResponse = await _supabase.From<Warehouse>().Get();
            ViewBag.Warehouses = warehousesResponse.Models ?? new List<Warehouse>();
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
            return RedirectToAction("Dashboard");
        }

        [HttpPost]
        public async Task<IActionResult> UpdateWarehouse(string id, string name, string address, double lat, double lng, bool isActive)
        {
            var warehouse = await _supabase.From<Warehouse>()
                .Filter("id", Operator.Equals, id)
                .Single();

            if (warehouse == null) return NotFound();

            warehouse.Name = name;
            warehouse.Address = address;
            warehouse.Lat = lat;
            warehouse.Lng = lng;
            warehouse.IsActive = isActive;
            await _supabase.From<Warehouse>().Update(warehouse);

            return RedirectToAction("Dashboard");
        }

        [HttpGet]
        public async Task<IActionResult> CreateAccount()
        {
            ViewBag.UserName = HttpContext.Session.GetString("UserName") ?? "Admin";
            var rolesResponse = await _supabase.From<Role>().Get();
            ViewBag.Roles = rolesResponse.Models?.Where(r => r.Name == "staff" || r.Name == "rider").ToList() ?? new List<Role>();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateAccount(string fullName, string email, string password, string phone, string roleName)
        {
            // Normalize phone: Keep only the last 10 digits
            var normalizedPhone = new string((phone ?? "").Where(char.IsDigit).ToArray());
            if (normalizedPhone.Length > 10) normalizedPhone = normalizedPhone.Substring(normalizedPhone.Length - 10);
            phone = normalizedPhone;

            var role = await _supabase.From<Role>()
                .Filter("name", Operator.Equals, roleName)
                .Single();

            if (role == null) return NotFound("Role not found.");

            var profile = new Profile
            {
                FullName = fullName,
                Email = email,
                Phone = phone,
                RoleId = role.Id,
                Password = password
            };

            await _supabase.From<Profile>().Insert(profile);

            return RedirectToAction("Dashboard");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteWarehouse(string id)
        {
            await _supabase.From<Warehouse>()
                .Filter("id", Operator.Equals, id)
                .Delete();

            return RedirectToAction("Dashboard");
        }

        [HttpPost]
        public async Task<IActionResult> CreateDetergent(string name, string brand, bool isActive)
        {
            var detergent = new Detergent
            {
                Name = name,
                Brand = brand,
                IsActive = isActive
            };

            await _supabase.From<Detergent>().Insert(detergent);
            return RedirectToAction("Dashboard");
        }

        [HttpPost]
        public async Task<IActionResult> ToggleDetergent(string id)
        {
            var detergent = await _supabase.From<Detergent>()
                .Filter("id", Operator.Equals, id)
                .Single();

            if (detergent == null) return NotFound();

            detergent.IsActive = !(detergent.IsActive ?? true);
            await _supabase.From<Detergent>().Update(detergent);

            return RedirectToAction("Dashboard");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteDetergent(string id)
        {
            await _supabase.From<Detergent>()
                .Filter("id", Operator.Equals, id)
                .Delete();

            return RedirectToAction("Dashboard");
        }
    }
}
