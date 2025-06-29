using BusTicketSystem.Data;
using BusTicketSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

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

        // Để hiển thị danh sách các công ty xe (nếu có và cần thiết)
        // public SelectList Companies { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var driver = await _context.Drivers.FirstOrDefaultAsync(m => m.DriverId == id);

            if (driver == null)
            {
                return NotFound();
            }

            // Không cho phép sửa nếu tài xế đã bị sa thải hoặc nghỉ việc
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

            // Nếu bạn muốn cho phép chọn công ty
            // Companies = new SelectList(_context.BusCompanies.OrderBy(c => c.CompanyName), "CompanyId", "CompanyName", driver.CompanyId);

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                // Nếu bạn muốn cho phép chọn công ty, tải lại danh sách
                // Companies = new SelectList(_context.BusCompanies.OrderBy(c => c.CompanyName), "CompanyId", "CompanyName", DriverInput.CompanyId);
                return Page();
            }

            var driverToUpdate = await _context.Drivers.FindAsync(DriverInput.DriverId);

            if (driverToUpdate == null)
            {
                return NotFound();
            }

            // Không cho phép sửa nếu tài xế đã bị sa thải hoặc nghỉ việc (kiểm tra lại phòng trường hợp)
            if (driverToUpdate.Status == DriverStatus.Terminated || driverToUpdate.Status == DriverStatus.Resigned)
            {
                TempData["ErrorMessage"] = "Không thể chỉnh sửa thông tin tài xế đã nghỉ việc hoặc bị sa thải.";
                // Companies = new SelectList(_context.BusCompanies.OrderBy(c => c.CompanyName), "CompanyId", "CompanyName", DriverInput.CompanyId);
                return Page(); // Hoặc RedirectToPage("./Index")
            }

            driverToUpdate.Fullname = DriverInput.Fullname;
            driverToUpdate.Email = DriverInput.Email;
            driverToUpdate.Phone = DriverInput.Phone;
            driverToUpdate.LicenseNumber = DriverInput.LicenseNumber; // Cẩn thận nếu đây là UNIQUE
            driverToUpdate.DateOfBirth = DriverInput.DateOfBirth;
            driverToUpdate.Address = DriverInput.Address;
            driverToUpdate.Status = DriverInput.Status;
            driverToUpdate.CompanyId = DriverInput.CompanyId.Value; // Nếu có
            driverToUpdate.UpdatedAt = DateTime.UtcNow;

            try
            {
                var notification = new Notification
                {
                    Message = $"Thông tin của tài xế '{driverToUpdate.Fullname}' vừa được cập nhật",
                    Category = NotificationCategory.Driver,
                    TargetUrl = Url.Page("/ForAdmin/DriverManage/Index", new { id = driverToUpdate.DriverId }),
                    IconCssClass = "bi bi-pencil-square",
                    RecipientUserId = null // Assuming AdminRecipientId is the correct property name
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
                else
                {
                    ModelState.AddModelError(string.Empty, "Có lỗi xảy ra khi cập nhật dữ liệu. Vui lòng thử lại.");
                    // Companies = new SelectList(_context.BusCompanies.OrderBy(c => c.CompanyName), "CompanyId", "CompanyName", DriverInput.CompanyId);
                    return Page();
                }
            }
            catch (DbUpdateException) // Bắt lỗi nếu LicenseNumber bị trùng (UNIQUE constraint)
            {
                // Kiểm tra InnerException để xem có phải lỗi UNIQUE constraint không
                // Ví dụ: if (ex.InnerException is MySqlException mysqlEx && mysqlEx.Number == 1062)
                ModelState.AddModelError("DriverInput.LicenseNumber", "Số bằng lái này đã tồn tại.");
                // Companies = new SelectList(_context.BusCompanies.OrderBy(c => c.CompanyName), "CompanyId", "CompanyName", DriverInput.CompanyId);
                return Page();
            }
        }
    }
}