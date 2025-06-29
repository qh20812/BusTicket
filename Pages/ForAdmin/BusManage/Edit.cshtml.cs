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

        // Dùng để populate dropdownlist Công ty
        public SelectList Companies { get; set; } = new SelectList(Enumerable.Empty<SelectListItem>());

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            // Load danh sách công ty cho dropdown
            await LoadCompaniesAsync();

            if (id == null)
            {
                // Thêm mới
                ViewData["Title"] = "Thêm mới Xe buýt";
                return Page();
            }

            // Chỉnh sửa
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
            // Load lại danh sách công ty nếu ModelState không hợp lệ để giữ dropdown
            await LoadCompaniesAsync();

            if (!ModelState.IsValid)
            {
                // Đặt lại tiêu đề cho trường hợp thêm mới khi có lỗi validation
                if (BusInput.BusId == 0)
                {
                    ViewData["Title"] = "Thêm mới Xe buýt";
                }
                else
                {
                    ViewData["Title"] = "Chỉnh sửa Xe buýt";
                }
                return Page();
            }

            if (BusInput.BusId == 0)
            {
                // Thêm mới
                var newBus = new Bus
                {
                    LicensePlate = BusInput.LicensePlate,
                    BusType = BusInput.BusType,
                    Capacity = BusInput.Capacity,
                    CompanyId = BusInput.CompanyId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                // Kiểm tra trùng biển số trước khi thêm
                if (await _context.Buses.AnyAsync(b => b.LicensePlate == newBus.LicensePlate))
                {
                    ModelState.AddModelError("BusInput.LicensePlate", "Biển số xe này đã tồn tại.");
                    ViewData["Title"] = "Thêm mới Xe buýt"; // Đặt lại tiêu đề
                    return Page();
                }

                _context.Buses.Add(newBus);
                TempData["SuccessMessage"] = $"Đã thêm mới xe '{newBus.LicensePlate}' thành công.";
            }
            else
            {
                // Chỉnh sửa
                var busToUpdate = await _context.Buses.FindAsync(BusInput.BusId);

                if (busToUpdate == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy xe cần cập nhật.";
                    return RedirectToPage("./Index");
                }

                // Kiểm tra trùng biển số với xe khác (trừ chính nó)
                if (await _context.Buses.AnyAsync(b => b.LicensePlate == BusInput.LicensePlate && b.BusId != BusInput.BusId))
                {
                    ModelState.AddModelError("BusInput.LicensePlate", "Biển số xe này đã tồn tại ở xe khác.");
                    ViewData["Title"] = "Chỉnh sửa Xe buýt"; // Đặt lại tiêu đề
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
                    // Load lại danh sách công ty nếu có lỗi DB
                    await LoadCompaniesAsync();
                    if (BusInput.BusId == 0)
                    {
                        ViewData["Title"] = "Thêm mới Xe buýt";
                    }
                    else
                    {
                        ViewData["Title"] = "Chỉnh sửa Xe buýt";
                    }
                    return Page();
                }
            }
            catch (DbUpdateException) // Bắt lỗi nếu LicenseNumber bị trùng (UNIQUE constraint)
            {
                // Kiểm tra InnerException để xem có phải lỗi UNIQUE constraint không
                // Đây là cách chung, bạn có thể cần điều chỉnh tùy thuộc vào loại database (MySQL, SQL Server, etc.)
                // Ví dụ cho MySQL: if (ex.InnerException is MySqlException mysqlEx && mysqlEx.Number == 1062)
                // Ví dụ cho SQL Server: if (ex.InnerException is SqlException sqlEx && sqlEx.Number == 2601)
                // Nếu không rõ loại lỗi, có thể kiểm tra message hoặc chỉ bắt chung DbUpdateException

                // Log lỗi chi tiết ex.InnerException.Message để debug
                ModelState.AddModelError("BusInput.LicensePlate", "Biển số xe này đã tồn tại.");
                // Load lại danh sách công ty nếu có lỗi DB
                await LoadCompaniesAsync();
                if (BusInput.BusId == 0)
                {
                    ViewData["Title"] = "Thêm mới Xe buýt";
                }
                else
                {
                    ViewData["Title"] = "Chỉnh sửa Xe buýt";
                }
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
            // Chỉ lấy các công ty có trạng thái 'active'
            var activeCompanies = await _context.BusCompanies
                                                .Where(c => c.Status == BusCompanyStatus.Active) // Giả định bạn có Enum BusCompanyStatus
                                                .OrderBy(c => c.CompanyName)
                                                .ToListAsync();
            Companies = new SelectList(activeCompanies, "CompanyId", "CompanyName");
        }
    }

    public class BusInputModel
    {
        public int BusId { get; set; } // Dùng 0 cho thêm mới

        [Required(ErrorMessage = "Biển số xe không được để trống.")]
        [StringLength(20, MinimumLength = 6, ErrorMessage = "Biển số xe phải có từ 6 đến 20 ký tự.")]
        [RegularExpression(@"^[A-Z0-9\-]+$", ErrorMessage = "Biển số xe chỉ được chứa chữ cái viết hoa, số và dấu gạch nối (ví dụ: 51A-12345 hoặc 30K99999).")]
        [Display(Name = "Biển số xe")]
        public string LicensePlate { get; set; } = string.Empty;

        [Required(ErrorMessage = "Loại xe không được để trống.")]
        [StringLength(50, MinimumLength = 5, ErrorMessage = "Loại xe phải có ít nhất 5 ký tự và không quá 50 ký tự.")]
        [RegularExpression(@"^[^\W\d_].*$", ErrorMessage = "Loại xe phải bắt đầu bằng một chữ cái và không chứa ký tự đặc biệt ở đầu.")]
        [Display(Name = "Loại xe")]
        public string BusType { get; set; } = string.Empty;

        [Required(ErrorMessage = "Trạng thái không được để trống.")]
        [Display(Name = "Trạng thái")]
        public BusStatus? Status { get; set; }

        [Required(ErrorMessage = "Sức chứa không được để trống.")]
        [Range(1, int.MaxValue, ErrorMessage = "Sức chứa phải lớn hơn 0.")]
        [Display(Name = "Sức chứa")]
        public int? Capacity { get; set; }

        [Display(Name = "Công ty")]
        public int? CompanyId { get; set; } // Cho phép null nếu xe không thuộc công ty nào
    }
}