namespace SmartWash.Services
{
    public interface IEmailService
    {
        Task SendStatusEmail(string toEmail, string customerName, string orderId, string service, string detergent, decimal? weight, decimal? total, string status);
    }
}
