using BusTicketSystem.Data;
using BusTicketSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace BusTicketSystem.Pages.ForAdmin.Message
{
    [Authorize(Roles = "Admin")]
    public class DetailsModel(AppDbContext context) : PageModel
    {
        private readonly AppDbContext _context = context;

        // Sử dụng trực tiếp Model.Message thay vì tạo ViewModel riêng cho trang Details
        // vì hầu hết các trường đều được hiển thị trực tiếp.
        public required Models.Message MessageDetail { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                TempData["ErrorMessage"] = "ID tin nhắn không hợp lệ.";
                return RedirectToPage("./Index");
            }

#pragma warning disable CS8601 // Possible null reference assignment.
            MessageDetail = await _context.Messages
                                    .Include(m => m.RepliedByUser) // Lấy thông tin người trả lời (nếu có)
                                    .Include(m => m.ClosedByUser)  // Lấy thông tin người đóng (nếu có)
                                    .FirstOrDefaultAsync(m => m.MessageId == id);
#pragma warning restore CS8601 // Possible null reference assignment.

            if (MessageDetail == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy tin nhắn.";
                return RedirectToPage("./Index");
            }
            return Page();
        }

        public async Task<IActionResult> OnPostMarkAsReadAsync(int id)
        {
            var message = await _context.Messages.FindAsync(id);
            if (message == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy tin nhắn.";
                return RedirectToPage("./Index");
            }

            if (message.Status == MessageStatus.Unread)
            {
                message.Status = MessageStatus.Read;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã đánh dấu tin nhắn là đã đọc.";
            }
            else
            {
                TempData["InfoMessage"] = "Tin nhắn này không ở trạng thái 'Chưa đọc'.";
            }
            // Chuyển hướng lại trang chi tiết của chính tin nhắn đó
            return RedirectToPage("./Details", new { id });
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var message = await _context.Messages.FindAsync(id);
            if (message != null)
            {
                _context.Messages.Remove(message);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã xóa tin nhắn.";
                return RedirectToPage("./Index"); // Xóa xong thì quay về danh sách
            }

            TempData["ErrorMessage"] = "Không tìm thấy tin nhắn để xóa.";
            // Nếu không tìm thấy để xóa, có thể quay về Index hoặc ở lại Details (nếu có ID)
            // Ở đây chọn quay về Index vì tin nhắn không tồn tại.
            return RedirectToPage("./Index");
        }
    }
}