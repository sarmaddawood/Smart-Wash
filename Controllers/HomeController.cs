using Microsoft.AspNetCore.Mvc;
using SmartWash.Models;
using static Postgrest.Constants;

namespace SmartWash.Controllers
{
    public class HomeController : Controller
    {
        private readonly Supabase.Client _supabase;

        public HomeController(Supabase.Client supabase)
        {
            _supabase = supabase;
        }

        public async Task<IActionResult> Index()
        {
            var response = await _supabase.From<SmartWash.Models.Service>().Get();
            ViewBag.Services = response.Models.Where(s => s.IsActive).ToList();
            ViewBag.UserName = HttpContext.Session.GetString("UserName");
            ViewBag.UserRole = HttpContext.Session.GetString("UserRole");

            var announcements = await _supabase.From<Announcement>()
                .Filter("is_active", Operator.Equals, "true")
                .Order("created_at", Ordering.Descending)
                .Get();
            ViewBag.Announcements = announcements.Models;

            return View();
        }
    }
}
