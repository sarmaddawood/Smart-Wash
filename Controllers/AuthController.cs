using Microsoft.AspNetCore.Mvc;
using SmartWash.Models;
using static Postgrest.Constants;

namespace SmartWash.Controllers
{
    public class AuthController : Controller
    {
        private readonly Supabase.Client _supabase;

        public AuthController(Supabase.Client supabase)
        {
            _supabase = supabase;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        {
            var profileResponse = await _supabase.From<Profile>()
                .Filter("email", Operator.Equals, email)
                .Get();

            var profile = profileResponse.Models.FirstOrDefault();

            if (profile == null || profile.Password != password)
            {
                ViewBag.Error = "Invalid email or password.";
                return View();
            }

            var roleResponse = await _supabase.From<Role>()
                .Filter("id", Operator.Equals, profile.RoleId)
                .Single();

            if (roleResponse == null)
            {
                ViewBag.Error = "User role not found.";
                return View();
            }

            HttpContext.Session.SetString("UserId", profile.Id);
            HttpContext.Session.SetString("UserName", profile.FullName);
            HttpContext.Session.SetString("UserEmail", profile.Email);
            HttpContext.Session.SetString("UserPhone", profile.Phone ?? "");
            HttpContext.Session.SetString("UserRole", roleResponse.Name);
            HttpContext.Session.SetString("RoleId", profile.RoleId);

            return roleResponse.Name switch
            {
                "admin" => RedirectToAction("Dashboard", "Admin"),
                "staff" => RedirectToAction("Dashboard", "Staff"),
                "rider" => RedirectToAction("Dashboard", "Rider"),
                _ => RedirectToAction("Dashboard", "Customer")
            };
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(string fullName, string email, string password, string phone)
        {
            // Normalize phone: Keep only the last 10 digits
            var normalizedPhone = new string((phone ?? "").Where(char.IsDigit).ToArray());
            if (normalizedPhone.Length > 10) normalizedPhone = normalizedPhone.Substring(normalizedPhone.Length - 10);
            phone = normalizedPhone;

            var customerRole = await _supabase.From<Role>()
                .Filter("name", Operator.Equals, "customer")
                .Single();

            if (customerRole == null)
            {
                ViewBag.Error = "Internal error: Customer role not found.";
                return View();
            }

            var profile = new Profile
            {
                FullName = fullName,
                Email = email,
                Phone = phone,
                RoleId = customerRole.Id,
                Password = password
            };

            await _supabase.From<Profile>().Insert(profile);

            var savedProfile = await _supabase.From<Profile>()
                .Filter("email", Operator.Equals, email)
                .Single();

            if (savedProfile == null)
            {
                ViewBag.Error = "Internal error: Failed to retrieve saved profile.";
                return View();
            }

            HttpContext.Session.SetString("UserId", savedProfile.Id);
            HttpContext.Session.SetString("UserName", fullName);
            HttpContext.Session.SetString("UserEmail", email);
            HttpContext.Session.SetString("UserPhone", phone);
            HttpContext.Session.SetString("UserRole", "customer");
            HttpContext.Session.SetString("RoleId", customerRole.Id);

            return RedirectToAction("Dashboard", "Customer");
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }
    }
}
