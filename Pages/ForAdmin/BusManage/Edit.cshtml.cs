using BusTicketSystem.Data;
using BusTicketSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace BusTicketSystem.Pages.ForAdmin.BusManage
{
    [Authorize(Roles = "Admin")]
    public class EditModel : PageModel
    {
        private readonly AppDbContext _context;

        public EditModel(AppDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public BusInputModel BusInput { get; set; } = new BusInputModel();

        public SelectList Companies { get; set; } = new SelectList(Enumerable.Empty<SelectListItem>());

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            await LoadCompaniesAsync();

            if (id == null)
            {
                ViewData["Title"] = "Thêm mới Xe buýt";
                return Page();
            }

            ViewData["Title"] = "Chỉnh sửa Xe buýt";
            var bus = await _context.Buses.FirstOrDefaultAsync(m => m.BusId == id);

            if (bus == null)
            {
                return NotFound();
            }

            BusInput = new BusInputModel
            {
                BusId = bus.BusId,
                LicensePlate = bus.LicensePlate,
                BusType = bus.BusType,
                Capacity = bus.Capacity,
                CompanyId = bus.CompanyId
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            await LoadCompaniesAsync();

            if (!ModelState.IsValid)
            {
                ViewData["Title"] = BusInput.BusId == 0 ? "Thêm mới Xe buýt" : "Chỉnh sửa Xe buýt";
                return Page();
            }

            if (BusInput.BusId == 0)
            {
                var newBus = new Bus
                {
                    LicensePlate = BusInput.LicensePlate,
                    BusType = BusInput.BusType,
                    Capacity = BusInput.Capacity,
                    CompanyId = BusInput.CompanyId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                if (await _context.Buses.AnyAsync(b => b.LicensePlate == newBus.LicensePlate))
                {
                    ModelState.AddModelError("BusInput.LicensePlate", "Biển số xe này đã tồn tại.");
                    ViewData["Title"] = "Thêm mới Xe buýt";
                    return Page();
                }

                _context.Buses.Add(newBus);
                TempData["SuccessMessage"] = $"Đã thêm mới xe '{newBus.LicensePlate}' thành công.";
            }
            else
            {
                var busToUpdate = await _context.Buses.FindAsync(BusInput.BusId);

                if (busToUpdate == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy xe cần cập nhật.";
                    return RedirectToPage("./Index");
                }

                if (await _context.Buses.AnyAsync(b => b.LicensePlate == BusInput.LicensePlate && b.BusId != BusInput.BusId))
                {
                    ModelState.AddModelError("BusInput.LicensePlate", "Biển số xe này đã tồn tại ở xe khác.");
                    ViewData["Title"] = "Chỉnh sửa Xe buýt";
                    return Page();
                }

                busToUpdate.LicensePlate = BusInput.LicensePlate;
                busToUpdate.BusType = BusInput.BusType;
                busToUpdate.Capacity = BusInput.Capacity;
                busToUpdate.CompanyId = BusInput.CompanyId;
                busToUpdate.UpdatedAt = DateTime.UtcNow;

                _context.Attach(busToUpdate).State = EntityState.Modified;
                TempData["SuccessMessage"] = $"Đã cập nhật xe '{busToUpdate.LicensePlate}' thành công.";
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!BusExists(BusInput.BusId))
                {
                    return NotFound();
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Có lỗi xảy ra khi cập nhật dữ liệu. Vui lòng thử lại.");
                    await LoadCompaniesAsync();
                    ViewData["Title"] = BusInput.BusId == 0 ? "Thêm mới Xe buýt" : "Chỉnh sửa Xe buýt";
                    return Page();
                }
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError("BusInput.LicensePlate", "Biển số xe này đã tồn tại.");
                await LoadCompaniesAsync();
                ViewData["Title"] = BusInput.BusId == 0 ? "Thêm mới Xe buýt" : "Chỉnh sửa Xe buýt";
                return Page();
            }

            return RedirectToPage("./Index");
        }

        private bool BusExists(int id)
        {
            return _context.Buses.Any(e => e.BusId == id);
        }

        private async Task LoadCompaniesAsync()
        {
            var activeCompanies = await _context.BusCompanies
                .Where(c => c.Status == BusCompanyStatus.Active)
                .OrderBy(c => c.CompanyName)
                .ToListAsync();
            Companies = new SelectList(activeCompanies, "CompanyId", "CompanyName");
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

        [Display(Name = "Công ty")]
        public int? CompanyId { get; set; }
    }
}