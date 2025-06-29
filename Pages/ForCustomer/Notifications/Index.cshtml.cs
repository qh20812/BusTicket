using BusTicketSystem.Data;
using BusTicketSystem.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BusTicketSystem.Pages.ForCustomer.Notifications
{
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public IndexModel(AppDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public IList<BusTicketSystem.Models.Notification> Notifications { get; set; } = new List<BusTicketSystem.Models.Notification>();

        private int? GetCurrentMemberId()
        {
            var userId = _httpContextAccessor.HttpContext?.Session.GetInt32("UserId");
            var role = _httpContextAccessor.HttpContext?.Session.GetString("Role");
            return (userId.HasValue && "member".Equals(role, System.StringComparison.OrdinalIgnoreCase)) ? userId : null;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var memberId = GetCurrentMemberId();
            if (!memberId.HasValue)
            {
                TempData["ErrorMessage"] = "Không thể xác định thông tin khách hàng. Vui lòng đăng nhập lại.";
                return Redirect("/Account/Login/Index");
            }
            var allowedCategories = new[]{
                NotificationCategory.Customer,
                NotificationCategory.Order,
                NotificationCategory.Trip,
                NotificationCategory.Registration
            };
            Notifications = await _context.Notifications.Where(n => n.RecipientUserId == memberId.Value && allowedCategories.Contains(n.Category)).OrderByDescending(n => n.CreatedAt).AsNoTracking().ToListAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostMarkAsReadAsync(int id)
        {
            var memberId = GetCurrentMemberId();
            if (!memberId.HasValue)
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập lại.";
                return Forbid();
            }
            var notification = await _context.Notifications.FirstOrDefaultAsync(n => n.NotificationId == id && n.RecipientUserId == memberId.Value);
            if (notification != null)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã đánh dấu thông báo là đã đọc.";
            }
            else
            {
                TempData["ErrorMessage"] = "Không tìm thấy thông báo hoặc bạn không có quyền.";
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostMarkAllAsReadAsync()
        {
            var memberId = GetCurrentMemberId();
            if (!memberId.HasValue)
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập lại.";
                return Forbid();
            }
            var unreadNotifications = await _context.Notifications.Where(n => n.RecipientUserId == memberId.Value && !n.IsRead).ToListAsync();
            foreach (var notification in unreadNotifications)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;
            }
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Đã đánh dấu tất cả thông báo là đã đọc.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var memberId = GetCurrentMemberId();
            if (!memberId.HasValue)
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập lại.";
                return Forbid();
            }
            var notification = await _context.Notifications.FirstOrDefaultAsync(n => n.NotificationId == id && n.RecipientUserId == memberId.Value);
            if (notification != null)
            {
                _context.Notifications.Remove(notification);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã xóa thông báo.";
            }
            else
            {
                TempData["ErrorMessage"] = "Không tìm thấy thông báo hoặc bạn không có quyền.";
            }
            return RedirectToPage();
        }
        public async Task<IActionResult> OnPostDeleteAllReadAsync()
        {
            var memberId = GetCurrentMemberId();
            if (!memberId.HasValue)
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập lại.";
                return Forbid();
            }
            var readNotifications = await _context.Notifications.Where(n => n.RecipientUserId == memberId.Value && n.IsRead).ToListAsync();
            _context.Notifications.RemoveRange(readNotifications);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Đã xóa tất cả thông báo đã đọc.";
            return RedirectToPage();
        }
    }
}