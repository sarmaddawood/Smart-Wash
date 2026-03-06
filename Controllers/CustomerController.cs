using Microsoft.AspNetCore.Mvc;
using SmartWash.Models;
using SmartWash.Services;
using static Postgrest.Constants;

namespace SmartWash.Controllers
{
    public class CustomerController : Controller
    {
        private readonly Supabase.Client _supabase;
        private readonly IEmailService _emailService;

        public CustomerController(Supabase.Client supabase, IEmailService emailService)
        {
            _supabase = supabase;
            _emailService = emailService;
        }

        public async Task<IActionResult> Dashboard()
        {
            var customerId = HttpContext.Session.GetString("UserId");
            ViewBag.UserName = HttpContext.Session.GetString("UserName");

            var ordersResponse = await _supabase.From<Order>()
                .Filter("customer_id", Operator.Equals, customerId)
                .Order("created_at", Ordering.Descending)
                .Get();

            var services = await _supabase.From<Service>().Get();
            var serviceDict = services.Models.ToDictionary(s => s.Id, s => s.Name);

            var reviews = await _supabase.From<Review>()
                .Filter("customer_id", Operator.Equals, customerId)
                .Get();
            var reviewedOrderIds = reviews.Models.Select(r => r.OrderId).ToHashSet();

            var allOrders = ordersResponse.Models;
            var activeStatuses = new[] { "Pending", "Rider Assigned", "Picked Up", "At Warehouse", "Washing", "Ready for Delivery", "Out for Delivery" };
            var activeOrders = allOrders.Where(o => activeStatuses.Contains(o.Status)).ToList();

            ViewBag.ActiveOrders = activeOrders;
            ViewBag.AllOrders = allOrders;
            ViewBag.ServiceDict = serviceDict;
            ViewBag.ReviewedOrderIds = reviewedOrderIds;

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CancelOrder(long id)
        {
            var order = await _supabase.From<Order>()
                .Filter("id", Operator.Equals, id.ToString())
                .Single();

            order.Status = "Cancelled";
            await _supabase.From<Order>().Update(order);

            var customerEmail = HttpContext.Session.GetString("UserEmail");
            var customerName = HttpContext.Session.GetString("UserName");

            var service = await _supabase.From<Service>()
                .Filter("id", Operator.Equals, order.ServiceId.ToString())
                .Single();

            await _emailService.SendStatusEmail(customerEmail, customerName, order.Id, service.Name, order.WeightKg, order.TotalPrice, "Cancelled");

            return RedirectToAction("Dashboard");
        }

        [HttpPost]
        public async Task<IActionResult> SubmitReview(long orderId, int rating, string comment)
        {
            var customerId = HttpContext.Session.GetString("UserId");

            var review = new Review
            {
                OrderId = orderId,
                CustomerId = customerId,
                Rating = rating,
                Comment = comment ?? ""
            };

            await _supabase.From<Review>().Insert(review);

            return RedirectToAction("Dashboard");
        }

        [HttpGet]
        public async Task<IActionResult> ViewReview(long orderId)
        {
            var review = await _supabase.From<Review>()
                .Filter("order_id", Operator.Equals, orderId.ToString())
                .Single();

            return Json(new { review.Rating, review.Comment, review.CreatedAt });
        }
    }
}
