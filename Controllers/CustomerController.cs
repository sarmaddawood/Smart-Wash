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
            var customerId = HttpContext.Session.GetString("UserId") ?? "";
            ViewBag.UserName = HttpContext.Session.GetString("UserName") ?? "Customer";

            var ordersResponse = await _supabase.From<Order>()
                .Filter("customer_id", Operator.Equals, customerId)
                .Order("created_at", Ordering.Descending)
                .Get();

            var services = await _supabase.From<Service>().Get();
            var serviceDict = services.Models.ToDictionary(s => s.Id, s => s.Name);

            var detergents = await _supabase.From<Detergent>().Get();
            var detergentDict = detergents.Models.ToDictionary(d => d.Id, d => d.Name);

            var conditioners = await _supabase.From<Conditioner>().Get();
            var conditionerDict = conditioners.Models.ToDictionary(c => c.Id, c => c.Name);

            var reviews = await _supabase.From<Review>()
                .Filter("customer_id", Operator.Equals, customerId)
                .Get();
            var reviewedOrderIds = reviews.Models.Select(r => r.OrderId).ToHashSet();

            var allOrders = ordersResponse.Models;
            var activeStatuses = new[] { "Pending", "Picked Up", "At Warehouse", "Weighed & Measured", "Washing", "Ready for Delivery", "Out for Delivery" };
            var activeOrders = allOrders.Where(o => activeStatuses.Contains(o.Status)).ToList();

            ViewBag.ActiveOrders = activeOrders;
            ViewBag.AllOrders = allOrders;
            ViewBag.ServiceDict = serviceDict;
            ViewBag.DetergentDict = detergentDict;
            ViewBag.ConditionerDict = conditionerDict;
            ViewBag.ReviewedOrderIds = reviewedOrderIds;

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CancelOrder(string id)
        {
            var order = await _supabase.From<Order>()
                .Filter("id", Operator.Equals, id)
                .Single();

            if (order == null) return NotFound();

            order.Status = "Cancelled";
            await _supabase.From<Order>().Update(order);

            var customerEmail = HttpContext.Session.GetString("UserEmail") ?? "";
            var customerName = HttpContext.Session.GetString("UserName") ?? "Customer";

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
                detergentName = detergent?.Name ?? "—";
            }

            var conditionerName = "—";
            if (!string.IsNullOrEmpty(order.ConditionerId))
            {
                var conditioner = await _supabase.From<Conditioner>()
                    .Filter("id", Operator.Equals, order.ConditionerId)
                    .Single();
                conditionerName = conditioner?.Name ?? "—";
            }

            await _emailService.SendStatusEmail(customerEmail, customerName, order.Id, service.Name, detergentName, order.WeightKg, order.TotalPrice, "Cancelled");

            return RedirectToAction("Dashboard");
        }

        [HttpPost]
        public async Task<IActionResult> SubmitReview(string orderId, int rating, string comment)
        {
            var customerId = HttpContext.Session.GetString("UserId") ?? "";

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
        public async Task<IActionResult> ViewReview(string orderId)
        {
            var review = await _supabase.From<Review>()
                .Filter("order_id", Operator.Equals, orderId)
                .Single();

            if (review == null) return NotFound();

            return Json(new { review.Rating, review.Comment, review.CreatedAt });
        }
    }
}
