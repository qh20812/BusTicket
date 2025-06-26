using BusTicketSystem.Data;
using BusTicketSystem.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BusTicketSystem.Pages.ForAdmin
{
    public class BasePageModel : PageModel
    {
        protected readonly AppDbContext _context;

        public BasePageModel(AppDbContext context)
        {
            _context = context;
        }

        public int MessageCount { get; set; }
        public List<Notification> RecentNotifications { get; set; } = new List<Notification>();

        // Hàm khởi tạo dữ liệu chung
        public async Task InitializeAsync()
        {
            // Đếm số thư chưa đọc
            MessageCount = await _context.Notifications.CountAsync(n => !n.IsRead);

            // Lấy thông báo gần đây
            RecentNotifications = await _context.Notifications
                .OrderByDescending(n => n.CreatedAt)
                .Take(5)
                .ToListAsync();
        }
    }
}