namespace SmartWash.Services
{
    public interface IEmailService
    {
        Task SendStatusEmail(string toEmail, string customerName, long orderId, string service, double weight, double total, string status);
    }
}
