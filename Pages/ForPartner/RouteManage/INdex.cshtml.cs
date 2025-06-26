using System.Security.Claims;
using BusTicketSystem.Data;
using BusTicketSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BusTicketSystem.Pages.ForPartner.RouteManage
{
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _db;
        public IndexModel(AppDbContext db)
        {
            _db = db;
        }
        public IList<Models.Route> PartnerRoutes { get; set; } = new List<Models.Route>();
        [TempData]
        public string? SuccessMessage { get; set; }
        [TempData]
        public string? ErrorMessage { get; set; }
        private async Task<int?> GetCurrentPartnerCompanyIdAsync()
        {
            await Task.CompletedTask;
            var companyIdClaim = User.FindFirstValue("CompanyId");
            if (int.TryParse(companyIdClaim, out int companyId))
            {
                return companyId;
            }
            return null;
        }
        public async Task<IActionResult> OnGetAsync()
        {
            var partnerCompanyId = await GetCurrentPartnerCompanyIdAsync();
            if (!partnerCompanyId.HasValue)
            {
                ErrorMessage = "Không thể xác định thông tin nhà xe của bạn. Vui lòng đăng nhập lại hoặc liên hệ quản trị viên.";
                return Page();
            }
            PartnerRoutes = await _db.Routes.Where(r => r.ProposedByCompanyId == partnerCompanyId.Value).OrderByDescending(r => r.UpdatedAt).AsNoTracking().ToListAsync();
            return Page();
        }
        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var partnerCompanyId = await GetCurrentPartnerCompanyIdAsync();
            if (!partnerCompanyId.HasValue)
            {
                return Forbid();
            }
            var route = await _db.Routes.FindAsync(id);
            if (route == null || route.ProposedByCompanyId != partnerCompanyId.Value)
            {
                ErrorMessage = "Không tìm thấy lộ trình hoặc bạn không có quyền xóa lộ trình này.";
                return RedirectToPage();
            }
            if (route.Status != RouteStatus.PendingApproval && route.Status != RouteStatus.Rejected)
            {
                ErrorMessage = "Bạn chỉ có thể xóa các đề xuất lộ trình đang chờ duyệt hoặc đã bị từ chối.";
                return RedirectToPage();
            }
            bool isInUseByTrip = await _db.Trips.AnyAsync(t => t.RouteId == id);
            if (isInUseByTrip)
            {
                ErrorMessage = $"Không thể xóa đề xuất lộ trình '{route.Departure} - {route.Destination}' vì nó đã được liên kết với một hoặc nhiều chuyến đi. Vui lòng kiểm tra lại.";
                return RedirectToPage();
            }
            try
            {
                _db.Routes.Remove(route);
                await _db.SaveChangesAsync();
                SuccessMessage = $"Đã xóa đề xuất lộ trình '{route.Departure} - {route.Destination}' thành công.";
            }
            catch (DbUpdateException ex)
            {
                ErrorMessage = $"Lỗi khi xóa lộ trình. Vui lòng thử lại. Chi tiết: {ex.InnerException?.Message ?? ex.Message}";
            }
            return RedirectToPage();
        }
    }
}