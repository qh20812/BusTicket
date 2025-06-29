using BusTicketSystem.Data;
using BusTicketSystem.Models; // For Bus, BusStatus
using BusTicketSystem.Pages.ForAdmin.BusManage; // For BusInputModel
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace BusTicketSystem.Pages.ForPartner.BusManage
{
    [Authorize(Roles = "Partner")]
    public class EditModel : PageModel
    {
        private readonly AppDbContext _context;

        public EditModel(AppDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public BusInputModel BusInput { get; set; } = new BusInputModel();

        public string PartnerCompanyName { get; set; } = string.Empty;

        private int GetCurrentPartnerBusCompanyId()
        {
            var busCompanyIdClaim = User.FindFirstValue("CompanyId");
            if (int.TryParse(busCompanyIdClaim, out int busCompanyId))
            {
                return busCompanyId;
            }
            throw new System.Exception("BusCompanyId not found for the current partner.");
        }

        private async Task LoadPartnerCompanyName(int companyId)
        {
            var company = await _context.BusCompanies.FindAsync(companyId);
            PartnerCompanyName = company?.CompanyName ?? "Không xác định";
        }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            int partnerBusCompanyId = GetCurrentPartnerBusCompanyId();
            await LoadPartnerCompanyName(partnerBusCompanyId);

            if (id == null)
            {
                // Thêm mới by Partner
                ViewData["Title"] = "Yêu cầu thêm mới Xe buýt";
                BusInput.CompanyId = partnerBusCompanyId; // Set CompanyId for new bus
                BusInput.Status = BusStatus.PendingApproval; // Default status for new bus by partner
                return Page();
            }

            // Chỉnh sửa by Partner
            ViewData["Title"] = "Chỉnh sửa Xe buýt";
            var bus = await _context.Buses.FirstOrDefaultAsync(m => m.BusId == id && m.CompanyId == partnerBusCompanyId);

            if (bus == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy xe hoặc bạn không có quyền chỉnh sửa xe này.";
                return RedirectToPage("./Index");
            }

            BusInput = new BusInputModel
            {
                BusId = bus.BusId,
                LicensePlate = bus.LicensePlate,
                BusType = bus.BusType,
                Capacity = bus.Capacity, // BusInput.Capacity is now int?, bus.Capacity is int?
                CompanyId = bus.CompanyId, // Should be partner's companyId
                Status = bus.Status
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            int partnerBusCompanyId = GetCurrentPartnerBusCompanyId();
            await LoadPartnerCompanyName(partnerBusCompanyId); // For display if validation fails

            // Ensure CompanyId is always the partner's company ID and not tampered with
            BusInput.CompanyId = partnerBusCompanyId;

            if (!ModelState.IsValid)
            {
                ViewData["Title"] = BusInput.BusId == 0 ? "Yêu cầu thêm mới Xe buýt" : "Chỉnh sửa Xe buýt";
                return Page();
            }

            if (BusInput.BusId == 0)
            {
                // Thêm mới by Partner - goes to PendingApproval
                var newBus = new Bus
                {
                    LicensePlate = BusInput.LicensePlate,
                    BusType = BusInput.BusType,
                    Capacity = BusInput.Capacity,
                    CompanyId = partnerBusCompanyId, // Explicitly set partner's company
                    Status = BusStatus.PendingApproval, // New buses from partners are pending
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                if (await _context.Buses.AnyAsync(b => b.LicensePlate == newBus.LicensePlate))
                {
                    ModelState.AddModelError("BusInput.LicensePlate", "Biển số xe này đã tồn tại trong hệ thống.");
                    ViewData["Title"] = "Yêu cầu thêm mới Xe buýt";
                    return Page();
                }

                _context.Buses.Add(newBus);
                TempData["SuccessMessage"] = $"Đã gửi yêu cầu thêm mới xe '{newBus.LicensePlate}'. Chờ quản trị viên duyệt.";
            }
            else
            {
                // Chỉnh sửa by Partner
                var busToUpdate = await _context.Buses.FirstOrDefaultAsync(b => b.BusId == BusInput.BusId && b.CompanyId == partnerBusCompanyId);

                if (busToUpdate == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy xe cần cập nhật hoặc bạn không có quyền.";
                    return RedirectToPage("./Index");
                }

                // Partners cannot change status from PendingApproval or Rejected.
                // They can change details if Active, or set to Maintenance/Inactive.
                if (busToUpdate.Status == BusStatus.PendingApproval || busToUpdate.Status == BusStatus.Rejected)
                {
                    // Only allow updating non-status fields if pending/rejected
                    // Or, prevent editing entirely if pending/rejected - depends on business rule
                    // For now, let's assume they can update details but not status
                    BusInput.Status = busToUpdate.Status; // Keep original status
                }

                if (await _context.Buses.AnyAsync(b => b.LicensePlate == BusInput.LicensePlate && b.BusId != BusInput.BusId))
                {
                    ModelState.AddModelError("BusInput.LicensePlate", "Biển số xe này đã tồn tại ở xe khác trong hệ thống.");
                    ViewData["Title"] = "Chỉnh sửa Xe buýt";
                    return Page();
                }

                busToUpdate.LicensePlate = BusInput.LicensePlate;
                busToUpdate.BusType = BusInput.BusType;
                busToUpdate.Capacity = BusInput.Capacity;
                // Partner cannot change CompanyId. It's already filtered.
                // Status change logic:
                if (busToUpdate.Status != BusStatus.PendingApproval && busToUpdate.Status != BusStatus.Rejected)
                {
                    busToUpdate.Status = BusInput.Status.Value; // Allow status change if not pending/rejected
                }
                busToUpdate.UpdatedAt = DateTime.UtcNow;

                _context.Attach(busToUpdate).State = EntityState.Modified;
                TempData["SuccessMessage"] = $"Đã cập nhật xe '{busToUpdate.LicensePlate}' thành công.";
                if (busToUpdate.Status == BusStatus.PendingApproval) TempData["SuccessMessage"] += " Thay đổi sẽ có hiệu lực sau khi quản trị viên duyệt lại (nếu cần).";
            }

            await _context.SaveChangesAsync();
            return RedirectToPage("./Index");
        }
    }
}