using BusTicketSystem.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace BusTicketSystem.ViewComponents
{
    public class UnreadNotificationCountViewComponent : ViewComponent
    {
        private readonly AppDbContext _context;
        // private readonly IHttpContextAccessor _httpContextAccessor; // Nếu cần lấy AdminId từ session

        public UnreadNotificationCountViewComponent(AppDbContext context /*, IHttpContextAccessor httpContextAccessor*/)
        {
            _context = context;
            // _httpContextAccessor = httpContextAccessor;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            // Lấy AdminId từ session nếu thông báo được cá nhân hóa
            // var adminId = _httpContextAccessor.HttpContext?.Session.GetString("AdminUserId");
            // Hiện tại, đếm tất cả thông báo chưa đọc.
            var unreadCount = await _context.Notifications.CountAsync(n => !n.IsRead /* && n.AdminRecipientId == adminId */);
            return View(unreadCount); // Sẽ tìm Views/Shared/Components/UnreadNotificationCount/Default.cshtml
        }
    }
}