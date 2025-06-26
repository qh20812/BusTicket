using BusTicketSystem.Data;
using BusTicketSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BusTicketSystem.Pages.Home.ContactUs
{
    public class IndexModel(AppDbContext context) : PageModel
    {
        private readonly AppDbContext _context = context;

        [BindProperty]
        public required Message Input { get; set; }
        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }
        public void OnGet()
        {

        }
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                ErrorMessage = "Vui lòng kiểm tra lại các trường thông tin.";
                return Page();
            }
            Input.Status = MessageStatus.Unread;
            Input.SentAt = DateTime.UtcNow;
            _context.Messages.Add(Input);
            await _context.SaveChangesAsync();
            SuccessMessage = "Cảm ơn bạn đã liên hệ! Chúng tôi sẽ phản hồi trong thời gian sớm nhất.";
            ModelState.Clear();
            Input = new Message();
            return Page();
        }
    }
}