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
            ViewBag.Services = response.Models?.Where(s => s.IsActive).ToList() ?? new List<SmartWash.Models.Service>();
            ViewBag.UserName = HttpContext.Session.GetString("UserName") ?? "Guest";
            ViewBag.UserRole = HttpContext.Session.GetString("UserRole") ?? "Guest";

            var announcementsResponse = await _supabase.From<Announcement>()
                .Filter("is_active", Operator.Equals, "true")
                .Order("created_at", Ordering.Descending)
                .Get();
            ViewBag.Announcements = announcementsResponse.Models ?? new List<Announcement>();

            return View();
        }
    }
}
