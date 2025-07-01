using BusTicketSystem.Data;
using BusTicketSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace BusTicketSystem.Pages.ForPartner.TripStopsManage
{
    [Authorize(Roles = "Partner")]
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;

        public IndexModel(AppDbContext context)
        {
            _context = context;
        }

        public IList<Stop> Stops { get; set; } = new List<Stop>();
        [BindProperty(SupportsGet = true)]
        public string? SearchString { get; set; }

        private int GetCurrentPartnerBusCompanyId()
        {
            var busCompanyIdClaim = User.FindFirstValue("CompanyId");
            if (int.TryParse(busCompanyIdClaim, out int busCompanyId))
            {
                return busCompanyId;
            }
            throw new System.Exception("BusCompanyId not found for the current partner.");
        }

        public async Task OnGetAsync()
        {
            int partnerBusCompanyId = GetCurrentPartnerBusCompanyId();

            var query = _context.Stops
                .Where(s => s.CompanyId == partnerBusCompanyId || (s.CompanyId == null && s.Status == StopStatus.Approved))
                .AsQueryable();

            if (!string.IsNullOrEmpty(SearchString))
            {
                query = query.Where(s => s.StopName.Contains(SearchString) || (s.Address != null && s.Address.Contains(SearchString)));
            }

            Stops = await query.OrderByDescending(s => s.CreatedAt).ToListAsync();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            int partnerBusCompanyId = GetCurrentPartnerBusCompanyId();
            var stop = await _context.Stops
                .FirstOrDefaultAsync(s => s.StopId == id && (s.CompanyId == partnerBusCompanyId || s.CompanyId == null));

            if (stop == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy trạm dừng hoặc bạn không có quyền xóa.";
                return RedirectToPage("./Index");
            }

            if (stop.Status != StopStatus.PendingApproval && stop.Status != StopStatus.Rejected)
            {
                TempData["ErrorMessage"] = "Chỉ có thể xóa trạm dừng ở trạng thái Chờ duyệt hoặc Bị từ chối.";
                return RedirectToPage("./Index");
            }

            if (await _context.TripStops.AnyAsync(ts => ts.StopId == stop.StopId))
            {
                TempData["ErrorMessage"] = "Không thể xóa trạm dừng vì đang được sử dụng trong chuyến đi.";
                return RedirectToPage("./Index");
            }

            _context.Stops.Remove(stop);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Đã xóa trạm dừng thành công.";
            return RedirectToPage("./Index");
        }
    }
}