using System.ComponentModel.DataAnnotations;
using System.Reflection;
using BusTicketSystem.Data;
using BusTicketSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace BusTicketSystem.Pages.ForAdmin.Message
{
    [Authorize(Roles = "Admin")]
    public class IndexModel(AppDbContext context) : PageModel
    {
        private readonly AppDbContext _context = context;

        public List<MessageViewModel> MessagesDisplay { get; set; } = new List<MessageViewModel>();
        [BindProperty(SupportsGet = true)]
        [Display(Name = "Nội dung tìm kiếm")]
        [StringLength(100, ErrorMessage = "{0} không được vượt quá {1} ký tự.")]
        public string? SearchTerm { get; set; }
        [BindProperty(SupportsGet = true)]
        public string? FilterStatus { get; set; }
        [BindProperty(SupportsGet = true)]
        public string? SortOrder { get; set; }
        public SelectList Statuses { get; set; } = new SelectList(Enumerable.Empty<SelectListItem>());

        private void PopulateDropdowns()
        {
            var enumValues = Enum.GetValues(typeof(MessageStatus)).Cast<MessageStatus>();
            Statuses = new SelectList(enumValues.Select(e => new SelectListItem
            {
                Value = e.ToString(),
                Text = e.GetType().GetField(e.ToString())?.GetCustomAttribute<DisplayAttribute>()?.GetName() ?? e.ToString()
            }), "Value", "Text", FilterStatus);
        }

        public async Task OnGetAsync()
        {
            IQueryable<BusTicketSystem.Models.Message> query = _context.Messages;

            if (!string.IsNullOrEmpty(SearchTerm))
            {
                query = query.Where(m =>
                    m.Subject.Contains(SearchTerm) ||
                    m.SenderName.Contains(SearchTerm) ||
                    m.SenderEmail.Contains(SearchTerm));
            }

            if (!string.IsNullOrEmpty(FilterStatus) && Enum.TryParse<MessageStatus>(FilterStatus, out var statusEnum))
            {
                query = query.Where(m => m.Status == statusEnum);
            }

            switch (SortOrder?.ToLower())
            {
                case "oldest":
                    query = query.OrderBy(m => m.SentAt);
                    break;
                default: // "newest" or any other/null value
                    query = query.OrderByDescending(m => m.SentAt);
                    break;
            }

            var messages = await query.ToListAsync();
            MessagesDisplay = messages.Select(m => new MessageViewModel(m)).ToList();

            PopulateDropdowns();

            if (!ModelState.IsValid)
            {
                // Nếu ModelState không hợp lệ, MessagesDisplay có thể sẽ trống hoặc không được tải lại từ DB
                // Chúng ta vẫn cần PopulateDropdowns để các dropdown hoạt động đúng
                return; // Page() sẽ được trả về, hiển thị lỗi validation
            }
        }
        public async Task<IActionResult> OnPostMarkAsReadAsync(int id)
        {
            var message = await _context.Messages.FindAsync(id);
            if (message != null && message.Status == MessageStatus.Unread)
            {
                message.Status = MessageStatus.Read;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã đánh dấu tin nhắn là đã đọc.";
            }
            else if (message != null && message.Status != MessageStatus.Unread)
            {
                TempData["ErrorMessage"] = "Tin nhắn không ở trạng thái 'Chưa đọc'.";
            }
            else
            {
                TempData["ErrorMessage"] = "Không tìm thấy tin nhắn.";
            }
            return RedirectToPage(new { SearchTerm, FilterStatus, SortOrder });
        }
        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var message = await _context.Messages.FindAsync(id);
            if (message != null)
            {
                _context.Messages.Remove(message);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã xóa tin nhắn.";
            }
            else
            {
                TempData["ErrorMessage"] = "Không tìm thấy tin nhắn.";
            }
            return RedirectToPage(new { SearchTerm, FilterStatus, SortOrder });
        }
        public class MessageViewModel
        {
            public int Id { get; set; }
            public string SenderName { get; set; } = string.Empty;
            public string SenderEmail { get; set; } = string.Empty;
            public string Subject { get; set; } = string.Empty;
            public string ShortContent { get; set; } = string.Empty;
            public DateTime SentAt { get; set; }
            public string StatusDisplayName { get; set; } = string.Empty;
            public MessageStatus Status { get; set; }
            public MessageViewModel(BusTicketSystem.Models.Message m)
            {
                Id = m.MessageId;
                SenderName = m.SenderName;
                SenderEmail = m.SenderEmail;
                Subject = m.Subject;
                ShortContent = m.Content.Length > 100 ? m.Content.Substring(0, 100) + "..." : m.Content;
                SentAt = m.SentAt;
                Status = m.Status;
                StatusDisplayName = m.Status.GetType()
                                     .GetField(m.Status.ToString())?
                                     .GetCustomAttribute<DisplayAttribute>()?.GetName() ?? m.Status.ToString();
            }
        }
    }
}