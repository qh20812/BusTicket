// using Microsoft.Extensions.Options;
// using System.Collections.Generic;
// using System.Threading.Tasks;
// using MailerSendNetCore; // Sử dụng cho MailerSendAdvancedService
// using MailerSendNetCore.Common; // Sử dụng cho MailerSendRequest, EmailParameter, Recipient
// using RestSharp;
// using MailerSendNetCore.Emails;
// using System;

// namespace BusTicketSystem.Services
// {
//     public class MailerSendEmailSenderOptions
//     {
//         public string? ApiKey { get; set; }
//         public string? SenderEmail { get; set; }
//         public string? SenderName { get; set; }
//     }

//     public class MailerSendEmailSender : IEmailSender
//     {
//         private readonly MailerSendEmailSenderOptions _options;

//         public MailerSendEmailSender(IOptions<MailerSendEmailSenderOptions> optionsAccs)
//         {
//             _options = optionsAccs.Value;
//             // Xác thực cấu hình ngay khi khởi tạo
//             if (string.IsNullOrEmpty(_options.ApiKey))
//             {
//                 throw new ArgumentNullException(nameof(_options.ApiKey), "MailerSend ApiKey không được để trống.");
//             }
//             if (string.IsNullOrEmpty(_options.SenderEmail))
//             {
//                 throw new ArgumentNullException(nameof(_options.SenderEmail), "MailerSend SenderEmail không được để trống.");
//             }
//         }

//         public async Task SendEmailAsync(string email, string subject, string htmlMessage)
//         {
//             // ApiKey và SenderEmail đã được xác thực trong constructor
            
//             var mailerSendService = new MailerSend(_options.ApiKey!); // Sử dụng MailerSend. ApiKey được đảm bảo không null.
            
//             var fromAddress = new EmailAddress(_options.SenderEmail!, _options.SenderName ?? string.Empty); // SenderEmail được đảm bảo không null.
//             var toAddresses = new List<EmailAddress> { new EmailAddress(email) };
            
//             // Gọi phương thức SendMailAsync từ lớp MailerSend
//             // Tham số text_body có thể để null nếu chỉ gửi HTML
//             RestResponse response = await mailerSendService.SendMailAsync(
//                 to: toAddresses,
//                 from: fromAddress,
//                 subject: subject,
//                 html_body: htmlMessage,
//                 text_body: null 
//             );
            
//             if (!response.IsSuccessful)
//             {
//                 var errorMessage = $"Failed to send email via MailerSend. Status: {response.StatusCode}.";
//                 if (!string.IsNullOrEmpty(response.ErrorMessage))
//                 {
//                     errorMessage += $" ErrorMessage: {response.ErrorMessage}.";
//                 }
//                 if (!string.IsNullOrEmpty(response.Content))
//                 {
//                     errorMessage += $" Content: {response.Content}.";
//                 }
//                 throw new InvalidOperationException(errorMessage); // Sử dụng InvalidOperationException để cụ thể hóa lỗi
//             }
//         }
 
//         public Task SendPasswordResetEmailAsync(string email, string subject, string resetLinkMessage)
//         {
//             return SendEmailAsync(email, subject, resetLinkMessage);
//         }
//     }
// }