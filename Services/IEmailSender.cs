namespace BusTicketSystem.Services{
    public interface IEmailSender{
        Task SendEmailAsync(string email, string subject, string htmlMessage);
        Task SendPasswordResetEmailAsync(string email,string Subject,string resetLinkMessage);
    }
}