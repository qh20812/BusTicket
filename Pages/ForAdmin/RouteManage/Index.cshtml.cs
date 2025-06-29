using System.ComponentModel.DataAnnotations;
using BusTicketSystem.Data;
using BusTicketSystem.Models;
using BusTicketSystem.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace BusTicketSystem.Pages.ForAdmin.RouteManage{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;
        public IndexModel(AppDbContext context)
        {
            _context = context;
        }
        public IList<Models.Route> Routes { get; set; } = new List<Models.Route>();
        public IList<Models.Route> PendingApprovalRoutes { get; set; } = new List<Models.Route>();

        [TempData]
        public string? SuccessMessage { get; set; }
        [TempData]
        public string? ErrorMessage { get; set; }
        [BindProperty]
        public RouteActionInputModel RouteActionInput { get; set; } = new RouteActionInputModel();

        public async Task OnGetAsync()
        {
            Routes = await _context.Routes
                .Include(r => r.ProposedByCompany).OrderBy(r => r.Departure).ThenBy(r => r.Destination).AsNoTracking().ToListAsync();
            PendingApprovalRoutes = await _context.Routes
                .Include(r => r.ProposedByCompany)
                .Where(r => r.Status == RouteStatus.PendingApproval)
                .OrderByDescending(r => r.CreatedAt)
                .AsNoTracking()
                .ToListAsync();
        }
        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var route = await _context.Routes.FindAsync(id);
            if (route == null)
            {
                ErrorMessage = "Không tìm thấy lộ trình để xóa.";
                return RedirectToPage();
            }
            bool isInUseByTrip = await _context.Trips.AnyAsync(t => t.RouteId == id);
            if (isInUseByTrip)
            {
                ErrorMessage = $"Không thể xóa lộ trình '{route.Departure} - {route.Destination}' vì đang được sử dụng trong các chuyến đi. Vui lòng xóa các chuyến đi liên quan trước.";
                return RedirectToPage();
            }
            // bool isInUseByPromotion=await _context.Promotions.AnyAsync(p=>p.A==id);
            // if(isInUseByPromotion){
            //     ErrorMessage=$"Không thể xóa lộ trình '{route.Departure} - {route.Destination}' vì đang được áp dụng trong các khuyến mãi. Vui lòng gỡ bỏ lộ trình khởi các khuyến mãi liên quan trước.";
            //     return RedirectToPage();
            // }
            try
            {
                _context.Routes.Remove(route);
                await _context.SaveChangesAsync();
                SuccessMessage = $"Đã xóa lộ trình '{route.Departure} - {route.Destination}' thành công.";
            }
            catch (DbUpdateException ex)
            {
                ErrorMessage = $"Lỗi khi xóa lộ trình: {ex.InnerException?.Message ?? ex.Message}. Có thể lộ trình này đang được tham chiếu ở nơi khác.";
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostApproveRouteAsync(int id)
        {
            var route = await _context.Routes.FindAsync(id);
            if (route == null)
            {
                return NotFound();
            }

            if (route.Status != RouteStatus.PendingApproval)
            {
                ErrorMessage = "Lộ trình này không ở trạng thái chờ duyệt.";
                return RedirectToPage();
            }

            route.Status = RouteStatus.Approved;
            route.UpdatedAt = DateTime.UtcNow;
            _context.Attach(route).State = EntityState.Modified;

            var adminNotification = new Notification
            {
                Message = $"Lộ trình ID {route.RouteId} ({route.Departure} - {route.Destination}) đã được DUYỆT.",
                Category = NotificationCategory.System,
                TargetUrl = $"/ForAdmin/RouteManage/Edit/{route.RouteId}",
                IconCssClass = "bi bi-check-circle-fill",
            };
            _context.Notifications.Add(adminNotification);

            await _context.SaveChangesAsync();
            SuccessMessage = $"Đã duyệt lộ trình ID {id} ({route.Departure} - {route.Destination}).";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostRejectRouteAsync()
        {
            if (string.IsNullOrWhiteSpace(RouteActionInput.RejectionReason))
            {
                ModelState.AddModelError("RouteActionInput.RejectionReason", "Lý do từ chối không được để trống.");
            }

            if (!ModelState.IsValid)
            {
                await OnGetAsync();
                TempData["ShowRejectRouteModalOnError"] = RouteActionInput.RouteIdToAction.ToString();
                var firstError = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage;
                TempData["ActionError"] = $"Lỗi từ chối lộ trình: {firstError ?? "Dữ liệu không hợp lệ."}"; // Used by modal JS
                ErrorMessage = $"Lỗi từ chối lộ trình: {firstError ?? "Dữ liệu không hợp lệ."}"; // General error message
                return Page();
            }

            var route = await _context.Routes.FindAsync(RouteActionInput.RouteIdToAction);
            if (route == null) return NotFound();

            if (route.Status != RouteStatus.PendingApproval)
            {
                ErrorMessage = "Lộ trình này không ở trạng thái chờ duyệt.";
                return RedirectToPage();
            }

            route.Status = RouteStatus.Rejected;
            route.UpdatedAt = DateTime.UtcNow;
            _context.Attach(route).State = EntityState.Modified;

            var adminNotification = new Notification { Message = $"Lộ trình ID {route.RouteId} ({route.Departure} - {route.Destination}) đã bị TỪ CHỐI. Lý do: {RouteActionInput.RejectionReason}", Category = NotificationCategory.System, TargetUrl = $"/ForAdmin/RouteManage/Edit/{route.RouteId}", IconCssClass = "bi bi-x-circle-fill" };
            _context.Notifications.Add(adminNotification);
            await _context.SaveChangesAsync();
            SuccessMessage = $"Đã từ chối lộ trình ID {RouteActionInput.RouteIdToAction}. Lý do: {RouteActionInput.RejectionReason}";
            return RedirectToPage();
        }
    }
    public class RouteActionInputModel {
        [Required] public int RouteIdToAction { get; set; }
        [StringLength(500, ErrorMessage = "Lý do từ chối không được vượt quá 500 ký tự.")] public string RejectionReason { get; set; } = string.Empty;
    }
}