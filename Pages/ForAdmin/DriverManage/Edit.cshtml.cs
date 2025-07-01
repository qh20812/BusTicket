using BusTicketSystem.Data;
using BusTicketSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering; // Thêm directive này

namespace BusTicketSystem.Pages.ForAdmin.DriverManage
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
        public DriverInputModel DriverInput { get; set; } = new DriverInputModel();

        public SelectList Companies { get; set; } // Bỏ comment để khai báo Companies

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();
            var driver = await _context.Drivers.FirstOrDefaultAsync(m => m.DriverId == id);
            if (driver == null) return NotFound();

            if (driver.Status == DriverStatus.Terminated || driver.Status == DriverStatus.Resigned)
            {
                TempData["ErrorMessage"] = "Không thể chỉnh sửa thông tin tài xế đã nghỉ việc hoặc bị sa thải.";
                return RedirectToPage("./Index");
            }

            DriverInput = new DriverInputModel
            {
                DriverId = driver.DriverId,
                Fullname = driver.Fullname,
                Email = driver.Email,
                Phone = driver.Phone,
                LicenseNumber = driver.LicenseNumber,
                DateOfBirth = driver.DateOfBirth,
                Address = driver.Address,
                Status = driver.Status,
                CompanyId = driver.CompanyId
            };

            Companies = new SelectList(
                await _context.BusCompanies
                    .Where(c => c.Status == BusCompanyStatus.Active)
                    .OrderBy(c => c.CompanyName)
                    .Select(c => new { c.CompanyId, c.CompanyName })
                    .AsNoTracking()
                    .ToListAsync(),
                "CompanyId", "CompanyName", driver.CompanyId
            );

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                Companies = new SelectList(
                    await _context.BusCompanies
                        .Where(c => c.Status == BusCompanyStatus.Active)
                        .OrderBy(c => c.CompanyName)
                        .Select(c => new { c.CompanyId, c.CompanyName })
                        .AsNoTracking()
                        .ToListAsync(),
                    "CompanyId", "CompanyName", DriverInput.CompanyId
                );
                return Page();
            }

            var driverToUpdate = await _context.Drivers.FindAsync(DriverInput.DriverId);
            if (driverToUpdate == null) return NotFound();

            if (driverToUpdate.Status == DriverStatus.Terminated || driverToUpdate.Status == DriverStatus.Resigned)
            {
                TempData["ErrorMessage"] = "Không thể chỉnh sửa thông tin tài xế đã nghỉ việc hoặc bị sa thải.";
                Companies = new SelectList(
                    await _context.BusCompanies
                        .Where(c => c.Status == BusCompanyStatus.Active)
                        .OrderBy(c => c.CompanyName)
                        .Select(c => new { c.CompanyId, c.CompanyName })
                        .AsNoTracking()
                        .ToListAsync(),
                    "CompanyId", "CompanyName", DriverInput.CompanyId
                );
                return Page();
            }

            if (DriverInput.CompanyId.HasValue)
            {
                var company = await _context.BusCompanies.FindAsync(DriverInput.CompanyId);
                if (company == null || company.Status != BusCompanyStatus.Active)
                {
                    ModelState.AddModelError("DriverInput.CompanyId", "Nhà xe không hợp lệ hoặc không hoạt động.");
                    Companies = new SelectList(
                        await _context.BusCompanies
                            .Where(c => c.Status == BusCompanyStatus.Active)
                            .OrderBy(c => c.CompanyName)
                            .Select(c => new { c.CompanyId, c.CompanyName })
                            .AsNoTracking()
                            .ToListAsync(),
                        "CompanyId", "CompanyName", DriverInput.CompanyId
                    );
                    return Page();
                }
            }

            driverToUpdate.Fullname = DriverInput.Fullname;
            driverToUpdate.Email = DriverInput.Email;
            driverToUpdate.Phone = DriverInput.Phone;
            driverToUpdate.LicenseNumber = DriverInput.LicenseNumber;
            driverToUpdate.DateOfBirth = DriverInput.DateOfBirth;
            driverToUpdate.Address = DriverInput.Address;
            driverToUpdate.Status = DriverInput.Status;
            driverToUpdate.CompanyId = DriverInput.CompanyId; // Cho phép null
            driverToUpdate.UpdatedAt = DateTime.UtcNow;

            try
            {
                var notification = new Notification
                {
                    Message = $"Thông tin của tài xế '{driverToUpdate.Fullname}' vừa được cập nhật",
                    Category = NotificationCategory.Driver,
                    TargetUrl = Url.Page("/ForAdmin/DriverManage/Index", new { id = driverToUpdate.DriverId }),
                    IconCssClass = "bi bi-pencil-square",
                    RecipientUserId = null
                };
                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Thông tin tài xế đã được cập nhật thành công.";
                return RedirectToPage("./Index");
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Drivers.Any(e => e.DriverId == DriverInput.DriverId))
                {
                    return NotFound();
                }
                ModelState.AddModelError(string.Empty, "Có lỗi xảy ra khi cập nhật dữ liệu. Vui lòng thử lại.");
                Companies = new SelectList(
                    await _context.BusCompanies
                        .Where(c => c.Status == BusCompanyStatus.Active)
                        .OrderBy(c => c.CompanyName)
                        .Select(c => new { c.CompanyId, c.CompanyName })
                        .AsNoTracking()
                        .ToListAsync(),
                    "CompanyId", "CompanyName", DriverInput.CompanyId
                );
                return Page();
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError("DriverInput.LicenseNumber", "Số bằng lái này đã tồn tại.");
                Companies = new SelectList(
                    await _context.BusCompanies
                        .Where(c => c.Status == BusCompanyStatus.Active)
                        .OrderBy(c => c.CompanyName)
                        .Select(c => new { c.CompanyId, c.CompanyName })
                        .AsNoTracking()
                        .ToListAsync(),
                    "CompanyId", "CompanyName", DriverInput.CompanyId
                );
                return Page();
            }
        }
    }
}