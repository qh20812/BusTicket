using BusTicketSystem.Data;
using BusTicketSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using BusTicketSystem.Pages.ForAdmin.BusManage; // Thêm using cho BusViewModel
using System.ComponentModel.DataAnnotations; // Required for DisplayAttribute

namespace BusTicketSystem.Pages.ForPartner.BusManage
{
    [Authorize(Roles = "Partner")]
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;

        public IndexModel(AppDbContext context)
        {
            _context = context;
        }

        public IList<PartnerBusViewModel> Buses { get; set; } = new List<PartnerBusViewModel>();

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? SortOrder { get; set; }

        private int GetCurrentPartnerBusCompanyId()
        {
            var busCompanyIdClaim = User.FindFirstValue("CompanyId");
            if (int.TryParse(busCompanyIdClaim, out int busCompanyId))
            {
                return busCompanyId;
            }
            // This should ideally not happen if routing/authorization is correct
            throw new System.Exception("BusCompanyId not found for the current partner.");
        }

        public async Task<IActionResult> OnGetAsync()
        {
            int partnerBusCompanyId = GetCurrentPartnerBusCompanyId();

            IQueryable<Bus> query = _context.Buses
                                        .Where(b => b.CompanyId == partnerBusCompanyId)
                                        .Include(b => b.Company); // Company might be redundant if it's always the partner's

            if (!string.IsNullOrEmpty(SearchTerm))
            {
                query = query.Where(b => b.LicensePlate.Contains(SearchTerm) || b.BusType.Contains(SearchTerm));
            }

            // Basic sorting example, can be expanded like admin's page
            switch (SortOrder?.ToLower())
            {
                case "licenseplate_desc":
                    query = query.OrderByDescending(b => b.LicensePlate);
                    break;
                case "type":
                    query = query.OrderBy(b => b.BusType);
                    break;
                // Add more cases as needed
                default: // Default sort by creation date or license plate
                    query = query.OrderByDescending(b => b.CreatedAt);
                    break;
            }

            var busesFromDb = await query.ToListAsync();
            Buses = busesFromDb.Select(b => new PartnerBusViewModel(b)).ToList();
            return Page();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            int partnerBusCompanyId = GetCurrentPartnerBusCompanyId();
            var bus = await _context.Buses.FirstOrDefaultAsync(b => b.BusId == id && b.CompanyId == partnerBusCompanyId);

            if (bus == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy xe cần xóa hoặc bạn không có quyền xóa xe này.";
                return RedirectToPage();
            }

            // Add checks here: e.g., cannot delete if bus is in PendingApproval or has active trips
            if (bus.Status == BusStatus.PendingApproval)
            {
                TempData["ErrorMessage"] = $"Không thể xóa xe '{bus.LicensePlate}' khi đang chờ duyệt.";
                return RedirectToPage();
            }
            var hasTrips = await _context.Trips.AnyAsync(t => t.BusId == id && (t.Status == TripStatus.Scheduled || t.Status == TripStatus.PendingApproval));
            if (hasTrips)
            {
                TempData["ErrorMessage"] = $"Không thể xóa xe '{bus.LicensePlate}' vì nó đang được sử dụng trong các chuyến đi chưa hoàn thành.";
                return RedirectToPage();
            }

            _context.Buses.Remove(bus);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã xóa xe '{bus.LicensePlate}' thành công.";
            return RedirectToPage();
        }
    }

    public class PartnerBusViewModel : BusViewModel // Inherit from admin's BusViewModel or create a specific one
    {
        public string StatusDisplayName { get; set; }
        // Thuộc tính 'Status' giờ đây được kế thừa từ BusViewModel (sau khi đã được cập nhật)

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        public PartnerBusViewModel(Bus bus) : base(bus) // Call base constructor
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        {
            // 'this.Status' đã được thiết lập bởi constructor của lớp cơ sở (base(bus))
            var field = typeof(BusStatus).GetField(this.Status.ToString());
            var displayAttribute = field?.GetCustomAttributes(typeof(DisplayAttribute), false).FirstOrDefault() as DisplayAttribute;
#pragma warning disable CS8601 // Possible null reference assignment.
            StatusDisplayName = displayAttribute?.GetName() ?? Status.ToString();
#pragma warning restore CS8601 // Possible null reference assignment.
        }
    }
}