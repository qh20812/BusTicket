using BusTicketSystem.Data;
using BusTicketSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace BusTicketSystem.Pages.ForAdmin.Notifications
{
    public class IndexModel(AppDbContext context) : PageModel
    {
        private readonly AppDbContext _context = context;

        public IList<NotificationViewModel> NotificationsDisplay { get; set; } = new List<NotificationViewModel>();

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public NotificationCategory? FilterCategory { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? FilterReadStatus { get; set; } // "all", "read", "unread"

        [BindProperty(SupportsGet = true)]
        public string? SortOrder { get; set; } // "newest", "oldest"

        public required SelectList Categories { get; set; }

        public async Task OnGetAsync()
        {
            IQueryable<Notification> query = _context.Notifications;

            if (!string.IsNullOrEmpty(SearchTerm))
            {
                query = query.Where(n => n.Message.Contains(SearchTerm));
            }
            if (FilterCategory.HasValue)
            {
                query = query.Where(n => n.Category == FilterCategory.Value);
            }
            if (!string.IsNullOrEmpty(FilterReadStatus))
            {
                if (FilterReadStatus.Equals("read", StringComparison.OrdinalIgnoreCase))
                    query = query.Where(n => n.IsRead);
                else if (FilterReadStatus.Equals("unread", StringComparison.OrdinalIgnoreCase))
                    query = query.Where(n => !n.IsRead);
            }

            switch (SortOrder?.ToLower())
            {
                case "oldest":
                    query = query.OrderBy(n => n.CreatedAt);
                    break;
                default:
                    query = query.OrderByDescending(n => n.CreatedAt);
                    break;
            }

            var notifications = await query.ToListAsync();
            NotificationsDisplay = notifications.Select(n => new NotificationViewModel(n)).ToList();

            var enumValues = Enum.GetValues(typeof(NotificationCategory)).Cast<NotificationCategory>();
            Categories = new SelectList(enumValues.Select(e => new SelectListItem
            {
                Value = e.ToString(),
                Text = e.GetType().GetField(e.ToString())?.GetCustomAttribute<DisplayAttribute>()?.GetName() ?? e.ToString()
            }), "Value", "Text", FilterCategory?.ToString());
        }

        public async Task<IActionResult> OnPostMarkAsReadAsync(int id)
        {
            var notification = await _context.Notifications.FindAsync(id);
            if (notification != null)
            {
                notification.IsRead = true;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã đánh dấu thông báo là đã đọc.";
            }
            return RedirectToPage(new { SearchTerm, FilterCategory, FilterReadStatus, SortOrder });
        }

        public async Task<IActionResult> OnPostMarkAllAsReadAsync()
        {
            var unreadNotifications = await _context.Notifications.Where(n => !n.IsRead).ToListAsync();
            foreach (var notification in unreadNotifications)
            {
                notification.IsRead = true;
            }
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Tất cả thông báo chưa đọc đã được đánh dấu là đã đọc.";
            return RedirectToPage(new { SearchTerm, FilterCategory, FilterReadStatus, SortOrder });
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var notification = await _context.Notifications.FindAsync(id);
            if (notification != null)
            {
                _context.Notifications.Remove(notification);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã xóa thông báo.";
            }
            return RedirectToPage(new { SearchTerm, FilterCategory, FilterReadStatus, SortOrder });
        }

        public async Task<IActionResult> OnPostDeleteReadAsync()
        {
            var readNotifications = await _context.Notifications.Where(n => n.IsRead).ToListAsync();
            if (readNotifications.Any())
            {
                _context.Notifications.RemoveRange(readNotifications);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã xóa tất cả thông báo đã đọc.";
            }
            else
            {
                TempData["InfoMessage"] = "Không có thông báo nào đã đọc để xóa.";
            }
            return RedirectToPage(new { SearchTerm, FilterCategory, FilterReadStatus, SortOrder });
        }
    }

    public class NotificationViewModel
    {
        public int Id { get; set; }
        public string Message { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsRead { get; set; }
        public string CategoryDisplayName { get; set; }
        public string? TargetUrl { get; set; }
        public string IconCssClass { get; set; }

        public NotificationViewModel(Notification n)
        {
            Id = n.NotificationId;
            Message = n.Message;
            CreatedAt = n.CreatedAt;
            IsRead = n.IsRead;
            TargetUrl = n.TargetUrl;
            IconCssClass = n.IconCssClass ?? (n.Category == NotificationCategory.Customer ? "bi bi-person-fill" : "bi bi-bell-fill"); // Ví dụ icon
            CategoryDisplayName = n.Category.GetType().GetField(n.Category.ToString())?.GetCustomAttribute<DisplayAttribute>()?.GetName() ?? n.Category.ToString();
        }
    }
}