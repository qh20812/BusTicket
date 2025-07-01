using BusTicketSystem.Data;
using BusTicketSystem.Models;
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
                ViewData["Title"] = "Yêu cầu thêm mới Xe buýt";
                BusInput.CompanyId = partnerBusCompanyId;
                BusInput.Status = BusStatus.PendingApproval;
                return Page();
            }

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
                Capacity = bus.Capacity,
                CompanyId = bus.CompanyId,
                Status = bus.Status
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            int partnerBusCompanyId = GetCurrentPartnerBusCompanyId();
            await LoadPartnerCompanyName(partnerBusCompanyId);
            BusInput.CompanyId = partnerBusCompanyId;

            if (!ModelState.IsValid)
            {
                ViewData["Title"] = BusInput.BusId == 0 ? "Yêu cầu thêm mới Xe buýt" : "Chỉnh sửa Xe buýt";
                return Page();
            }

            if (BusInput.BusId == 0)
            {
                var newBus = new Bus
                {
                    LicensePlate = BusInput.LicensePlate,
                    BusType = BusInput.BusType,
                    Capacity = BusInput.Capacity,
                    CompanyId = partnerBusCompanyId,
                    Status = BusStatus.PendingApproval,
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
                var busToUpdate = await _context.Buses.FirstOrDefaultAsync(b => b.BusId == BusInput.BusId && b.CompanyId == partnerBusCompanyId);

                if (busToUpdate == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy xe cần cập nhật hoặc bạn không có quyền.";
                    return RedirectToPage("./Index");
                }

                if (busToUpdate.Status == BusStatus.PendingApproval || busToUpdate.Status == BusStatus.Rejected)
                {
                    BusInput.Status = busToUpdate.Status;
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
                if (busToUpdate.Status != BusStatus.PendingApproval && busToUpdate.Status != BusStatus.Rejected)
                {
                    busToUpdate.Status = BusInput.Status!.Value;
                }
                busToUpdate.UpdatedAt = DateTime.UtcNow;

                _context.Attach(busToUpdate).State = EntityState.Modified;
                TempData["SuccessMessage"] = $"Đã cập nhật xe '{busToUpdate.LicensePlate}' thành công.";
                if (busToUpdate.Status == BusStatus.PendingApproval) TempData["SuccessMessage"] += " Thay đổi sẽ có hiệu lực sau khi quản trị viên duyệt lại (nếu cần).";
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Buses.Any(e => e.BusId == BusInput.BusId))
                {
                    return NotFound();
                }
                ModelState.AddModelError(string.Empty, "Có lỗi xảy ra khi cập nhật dữ liệu. Vui lòng thử lại.");
                await LoadPartnerCompanyName(partnerBusCompanyId);
                ViewData["Title"] = BusInput.BusId == 0 ? "Yêu cầu thêm mới Xe buýt" : "Chỉnh sửa Xe buýt";
                return Page();
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError("BusInput.LicensePlate", "Biển số xe này đã tồn tại.");
                await LoadPartnerCompanyName(partnerBusCompanyId);
                ViewData["Title"] = BusInput.BusId == 0 ? "Yêu cầu thêm mới Xe buýt" : "Chỉnh sửa Xe buýt";
                return Page();
            }

            return RedirectToPage("./Index");
        }
    }

    public class BusInputModel
    {
        public int BusId { get; set; }

        [Required(ErrorMessage = "Biển số xe không được để trống.")]
        [StringLength(20, MinimumLength = 6, ErrorMessage = "Biển số xe phải có từ 6 đến 20 ký tự.")]
        [RegularExpression(@"^[A-Z0-9\-]+$", ErrorMessage = "Biển số xe chỉ được chứa chữ cái viết hoa, số và dấu gạch nối (ví dụ: 51A-12345 hoặc 30K99999).")]
        [Display(Name = "Biển số xe")]
        public string LicensePlate { get; set; } = string.Empty;

        [Required(ErrorMessage = "Loại xe không được để trống.")]
        [Display(Name = "Loại xe")]
        public string BusType { get; set; } = string.Empty;

        [Required(ErrorMessage = "Sức chứa không được để trống.")]
        [Range(1, 100, ErrorMessage = "Sức chứa phải từ 1 đến 100.")]
        [Display(Name = "Sức chứa")]
        public int? Capacity { get; set; }

        [Required(ErrorMessage = "Trạng thái không được để trống.")]
        [Display(Name = "Trạng thái")]
        public BusStatus? Status { get; set; }

        [Display(Name = "Công ty")]
        public int? CompanyId { get; set; }
    }
}