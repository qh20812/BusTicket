using BusTicketSystem.Data;
using BusTicketSystem.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

public interface IEmailSender
{
    Task SendEmailAsync(string toEmail, string subject, string message);
    Task SendPasswordResetEmailAsync(string email, string v1, string v2);
}

namespace BusTicketSystem.Pages.ForAdmin.Message
{
    public class ReplyModel(AppDbContext context, UserManager<User> userManager, IEmailSender emailSender) : PageModel
    {
        private readonly AppDbContext _context = context;
        private readonly UserManager<User> _userManager = userManager;
        private readonly IEmailSender _emailSender = emailSender;
        [BindProperty]
        public InputModel Input { get; set; } = new InputModel();
        public required Models.Message OriginalMessage { get; set; }
        public class InputModel
        {
            [Required]
            [EmailAddress]
            [Display(Name = "Người nhận (Email)")]
            public string ToEmail { get; set; } = string.Empty;
            [Required]
            [Display(Name = "Chủ đề")]
            public string Subject { get; set; } = string.Empty;
            [Required]
            [Display(Name = "Nội dung")]
            public string Content { get; set; } = string.Empty;

        }
        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                TempData["ErrorMessage"] = "ID tin nhắn không hợp lệ.";
                return RedirectToPage("./Index");
            }
#pragma warning disable CS8601 // Possible null reference assignment.
            OriginalMessage = await _context.Messages.FirstOrDefaultAsync(m => m.MessageId == id);
#pragma warning restore CS8601 // Possible null reference assignment.
            if (OriginalMessage == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy tin nhắn.";
                return RedirectToPage("./Index");
            }
            Input.ToEmail = OriginalMessage.SenderEmail;
            Input.Subject = $"Re: {OriginalMessage.Subject}";
            return Page();
        }
        public async Task<IActionResult> OnPostAsync(int id)
        {
            if (!ModelState.IsValid)
            {
#pragma warning disable CS8601 // Possible null reference assignment.
                OriginalMessage = await _context.Messages.FirstOrDefaultAsync(m => m.MessageId == id);
#pragma warning restore CS8601 // Possible null reference assignment.
                if (OriginalMessage == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy tin nhắn gốc khi trả lời.";
                    return RedirectToPage("./Index");

                }
                return Page();

            }
            var messageToReply = await _context.Messages.FirstOrDefaultAsync(m => m.MessageId == id);
            if (messageToReply == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy tin nhắn gốc để cập nhật trạng thái.";
                return RedirectToPage("./Index");

            }
            await _emailSender.SendEmailAsync(Input.ToEmail, Input.Subject, Input.Content);
            messageToReply.Status = MessageStatus.Replied;
            messageToReply.RepliedAt = DateTime.UtcNow;
            var currentUser = await _userManager.GetUserAsync(User);
            messageToReply.RepliedByUserId = currentUser?.UserId;
            messageToReply.AdminNotes += $"\n\n--- Reply by {currentUser?.Fullname ?? currentUser?.Email ?? "Admin"} at {DateTime.UtcNow:dd/MM/yyyy HH:mm:ss} ---\n{Input.Content}";
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Tin nhắn trả lời đã được gửi và trạng thái đã cập nhật.";
            return RedirectToPage("./Index");
        }
    }
}