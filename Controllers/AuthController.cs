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
            var session = await _supabase.Auth.SignIn(email, password);

            var profileResponse = await _supabase.From<Profile>()
                .Filter("id", Operator.Equals, session.User.Id)
                .Single();

            var roleResponse = await _supabase.From<Role>()
                .Filter("id", Operator.Equals, profileResponse.RoleId.ToString())
                .Single();

            HttpContext.Session.SetString("UserId", session.User.Id);
            HttpContext.Session.SetString("UserName", profileResponse.FullName);
            HttpContext.Session.SetString("UserEmail", profileResponse.Email);
            HttpContext.Session.SetString("UserPhone", profileResponse.Phone ?? "");
            HttpContext.Session.SetString("UserRole", roleResponse.Name);
            HttpContext.Session.SetString("RoleId", profileResponse.RoleId.ToString());

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
            var session = await _supabase.Auth.SignUp(email, password);

            var customerRole = await _supabase.From<Role>()
                .Filter("name", Operator.Equals, "customer")
                .Single();

            var profile = new Profile
            {
                Id = session.User.Id,
                FullName = fullName,
                Email = email,
                Phone = phone,
                RoleId = customerRole.Id
            };

            await _supabase.From<Profile>().Insert(profile);

            HttpContext.Session.SetString("UserId", session.User.Id);
            HttpContext.Session.SetString("UserName", fullName);
            HttpContext.Session.SetString("UserEmail", email);
            HttpContext.Session.SetString("UserPhone", phone);
            HttpContext.Session.SetString("UserRole", "customer");
            HttpContext.Session.SetString("RoleId", customerRole.Id.ToString());

            return RedirectToAction("Dashboard", "Customer");
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }
    }
}
