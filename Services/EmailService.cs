using System.Net;
using System.Net.Mail;

namespace SmartWash.Services
{
    public class EmailService : IEmailService
    {
        private readonly string _gmailUser;
        private readonly string _gmailAppPassword;
        private readonly string _fromName;
        private readonly bool _enabled;

        public EmailService(IConfiguration configuration)
        {
            _gmailUser = configuration["Email:GmailUser"] ?? string.Empty;
            _gmailAppPassword = configuration["Email:GmailAppPassword"] ?? string.Empty;
            _fromName = configuration["Email:FromName"] ?? "SmartWash";
            _enabled = configuration.GetValue<bool>("Email:Enabled");
        }

        public async Task SendStatusEmail(string toEmail, string customerName, string orderId, string service, string detergent, decimal? weight, decimal? total, string status)
        {
            if (!_enabled) return;
            var statusInfo = GetStatusInfo(status);
            var weightDisplay = weight.HasValue ? $"{weight.Value:F1} kg" : "Pending measurement";
            var totalDisplay = total.HasValue ? $"₱{total.Value:F2}" : "Pending measurement";

            var subject = statusInfo.Subject;
            var body = $@"
<!DOCTYPE html>
<html>
<head>
<meta charset='utf-8'>
<meta name='viewport' content='width=device-width, initial-scale=1.0'>
</head>
<body style='margin:0;padding:0;background-color:#f4f6f9;font-family:Arial,sans-serif;'>
<table width='100%' cellpadding='0' cellspacing='0' style='background-color:#f4f6f9;padding:40px 0;'>
<tr><td align='center'>
<table width='600' cellpadding='0' cellspacing='0' style='background-color:#ffffff;border-radius:12px;overflow:hidden;box-shadow:0 4px 24px rgba(0,0,0,0.08);'>
<tr><td style='background:linear-gradient(135deg,#0A1628,#2563EB);padding:32px 40px;text-align:center;'>
<h1 style='color:#ffffff;margin:0;font-size:28px;letter-spacing:1px;'>🫧 SmartWash</h1>
<p style='color:rgba(255,255,255,0.8);margin:4px 0 0;font-size:13px;'>Fresh Clothes. Zero Effort.</p>
</td></tr>
<tr><td style='padding:36px 40px;'>
<p style='font-size:16px;color:#333;margin:0 0 8px;'>Hi <strong>{customerName}</strong>,</p>
<p style='font-size:15px;color:#555;margin:0 0 24px;line-height:1.6;'>{statusInfo.Message}</p>
<table width='100%' cellpadding='0' cellspacing='0' style='background-color:#f8fafc;border-radius:8px;padding:20px;border:1px solid #e2e8f0;'>
<tr><td style='padding:8px 16px;'>
<table width='100%' cellpadding='0' cellspacing='0'>
<tr>
<td style='padding:8px 0;color:#64748b;font-size:13px;'>Order ID</td>
<td style='padding:8px 0;color:#0f172a;font-size:13px;font-weight:bold;text-align:right;'>#{orderId}</td>
</tr>
<tr>
<td style='padding:8px 0;color:#64748b;font-size:13px;border-top:1px solid #e2e8f0;'>Service</td>
<td style='padding:8px 0;color:#0f172a;font-size:13px;font-weight:bold;text-align:right;border-top:1px solid #e2e8f0;'>{service}</td>
</tr>
<tr>
<td style='padding:8px 0;color:#64748b;font-size:13px;border-top:1px solid #e2e8f0;'>Detergent</td>
<td style='padding:8px 0;color:#0f172a;font-size:13px;font-weight:bold;text-align:right;border-top:1px solid #e2e8f0;'>{detergent}</td>
</tr>
<tr>
<td style='padding:8px 0;color:#64748b;font-size:13px;border-top:1px solid #e2e8f0;'>Weight</td>
<td style='padding:8px 0;color:#0f172a;font-size:13px;font-weight:bold;text-align:right;border-top:1px solid #e2e8f0;'>{weightDisplay}</td>
</tr>
<tr>
<td style='padding:8px 0;color:#64748b;font-size:13px;border-top:1px solid #e2e8f0;'>Total</td>
<td style='padding:8px 0;color:#2563EB;font-size:15px;font-weight:bold;text-align:right;border-top:1px solid #e2e8f0;'>{totalDisplay}</td>
</tr>
<tr>
<td style='padding:8px 0;color:#64748b;font-size:13px;border-top:1px solid #e2e8f0;'>Status</td>
<td style='padding:8px 0;font-size:13px;font-weight:bold;text-align:right;border-top:1px solid #e2e8f0;'>
<span style='background-color:#2563EB;color:#ffffff;padding:4px 12px;border-radius:20px;font-size:12px;'>{status}</span>
</td>
</tr>
</table>
</td></tr>
</table>
</td></tr>
<tr><td style='background-color:#0A1628;padding:24px 40px;text-align:center;'>
<p style='color:rgba(255,255,255,0.6);font-size:12px;margin:0;'>© 2026 SmartWash Philippines. All rights reserved.</p>
<p style='color:rgba(255,255,255,0.4);font-size:11px;margin:4px 0 0;'>📞 +63 991 671 4363 | 📧 saldivarcharme@gmail.com</p>
</td></tr>
</table>
</td></tr>
</table>
</body>
</html>";

            var message = new MailMessage();
            message.From = new MailAddress(_gmailUser, _fromName);
            message.To.Add(new MailAddress(toEmail));
            message.Subject = subject;
            message.Body = body;
            message.IsBodyHtml = true;

            using var client = new SmtpClient("smtp.gmail.com", 587);
            client.Credentials = new NetworkCredential(_gmailUser, _gmailAppPassword);
            client.EnableSsl = true;

            await client.SendMailAsync(message);
        }

        private (string Subject, string Message) GetStatusInfo(string status)
        {
            return status switch
            {
                "Pending" => ("✅ Order Placed — SmartWash", "Your order has been placed successfully! We're preparing to pick up your laundry soon."),
                "Picked Up" => ("🏍️ Your laundry has been picked up", "Your laundry has been picked up by our rider and is heading to the warehouse."),
                "At Warehouse" => ("🏭 Your laundry arrived at our warehouse", "Your laundry has arrived at our warehouse and will be processed shortly."),
                "Weighed & Measured" => ("⚖️ Invoice Ready — here's your total", "Your laundry has been weighed and your invoice is ready. Check the details below!"),
                "Washing" => ("🫧 We're washing your clothes now", "Your clothes are being washed with care right now. Sit back and relax!"),
                "Ready for Delivery" => ("✨ Your laundry is clean & ready!", "Your laundry is fresh, clean, and ready to be delivered back to you!"),
                "Out for Delivery" => ("🚀 Your laundry is on its way back!", "Your clean laundry is on its way! Our rider is heading to your delivery address now."),
                "Delivered" => ("🎉 Delivered! Thank you for using SmartWash", "Your laundry has been delivered! Thank you for choosing SmartWash. We hope to serve you again soon!"),
                "Cancelled" => ("❌ Order Cancelled — SmartWash", "Your order has been cancelled. If this was a mistake, please place a new order. We're always here to help!"),
                _ => ($"Order Update — SmartWash", $"Your order status has been updated to: {status}")
            };
        }
    }
}
