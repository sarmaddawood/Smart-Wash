using Microsoft.AspNetCore.Mvc;
using SmartWash.Models;
using static Postgrest.Constants;

namespace SmartWash.Controllers
{
    public class ProfileController : Controller
    {
        private readonly Supabase.Client _supabase;

        public ProfileController(Supabase.Client supabase)
        {
            _supabase = supabase;
        }

        [HttpGet]
        public async Task<IActionResult> Edit()
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Auth");

            var profile = await _supabase.From<Profile>()
                .Filter("id", Operator.Equals, userId)
                .Single();

            if (profile == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            ViewBag.FullName = profile.FullName ?? "";
            ViewBag.Email = profile.Email ?? "";
            // Normalize phone for display: last 10 digits
            var phone = profile.Phone ?? "";
            if (phone.Length > 10) phone = phone.Substring(phone.Length - 10);
            ViewBag.Phone = phone;
            ViewBag.Password = profile.Password; 
            ViewBag.UserRole = HttpContext.Session.GetString("UserRole") ?? "User";
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Edit(string fullName, string email, string phone, string password)
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Auth");

            var profile = await _supabase.From<Profile>()
                .Filter("id", Operator.Equals, userId)
                .Single();

            if (profile == null)
            {
                return NotFound();
            }

            // Normalize phone: Keep only the last 10 digits
            var normalizedPhone = new string((phone ?? "").Where(char.IsDigit).ToArray());
            if (normalizedPhone.Length > 10) normalizedPhone = normalizedPhone.Substring(normalizedPhone.Length - 10);
            
            profile.FullName = fullName;
            profile.Email = email;
            profile.Phone = normalizedPhone;
            profile.Password = password;

            await _supabase.From<Profile>().Update(profile);

            HttpContext.Session.SetString("UserName", fullName);
            HttpContext.Session.SetString("UserEmail", email);
            HttpContext.Session.SetString("UserPhone", phone);

            var userRole = HttpContext.Session.GetString("UserRole");

            return userRole switch
            {
                "admin" => RedirectToAction("Dashboard", "Admin"),
                "staff" => RedirectToAction("Dashboard", "Staff"),
                "rider" => RedirectToAction("Dashboard", "Rider"),
                _ => RedirectToAction("Dashboard", "Customer")
            };
        }
    }
}
