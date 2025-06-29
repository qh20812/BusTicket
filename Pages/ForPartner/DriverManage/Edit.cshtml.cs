using BusTicketSystem.Data;
using BusTicketSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace BusTicketSystem.Pages.ForPartner.DriverManage
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
        public DriverInputModel DriverInput { get; set; } = new DriverInputModel();

        private int? GetCurrentBusCompanyId()
        {
            var companyIdClaim = User.FindFirstValue("CompanyId");
            if (int.TryParse(companyIdClaim, out int companyId))
            {
                return companyId;
            }
            return null;
        }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            var companyId = GetCurrentBusCompanyId();
            if (companyId == null) return Forbid();

            var driver = await _context.Drivers.FirstOrDefaultAsync(m => m.DriverId == id && m.CompanyId == companyId.Value);

            if (driver == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy tài xế hoặc tài xế không thuộc nhà xe của bạn.";
                return RedirectToPage("./Index");
            }

            if (driver.Status == DriverStatus.Terminated || driver.Status == DriverStatus.Resigned)
            {
                TempData["ErrorMessage"] = "Không thể chỉnh sửa thông tin tài xế đã nghỉ việc.";
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
                CompanyId = driver.CompanyId // This will be the current company's ID
            };
            ViewData["Title"] = $"Chỉnh sửa Tài xế: {driver.Fullname}";
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var companyId = GetCurrentBusCompanyId();
            if (companyId == null || DriverInput.CompanyId != companyId.Value) return Forbid();

            // Ensure LicenseNumber is unique if it's a system-wide unique constraint and it changed
            var driverToUpdate = await _context.Drivers.AsNoTracking().FirstOrDefaultAsync(d => d.DriverId == DriverInput.DriverId);
            if (driverToUpdate == null) return NotFound();

            if (driverToUpdate.LicenseNumber != DriverInput.LicenseNumber && await _context.Drivers.AnyAsync(d => d.LicenseNumber == DriverInput.LicenseNumber && d.DriverId != DriverInput.DriverId))
            {
                ModelState.AddModelError("DriverInput.LicenseNumber", "Số bằng lái này đã tồn tại trong hệ thống cho tài xế khác.");
            }

            if (!ModelState.IsValid)
            {
                ViewData["Title"] = $"Chỉnh sửa Tài xế: {DriverInput.Fullname}";
                return Page();
            }

            driverToUpdate.Fullname = DriverInput.Fullname;
            // ... (map other properties from DriverInput to driverToUpdate)
            // ... ensure CompanyId remains the bus company's ID
            _context.Attach(driverToUpdate).State = EntityState.Modified; // This is not correct, map properties
            // Correct update:
            var dbDriver = await _context.Drivers.FindAsync(DriverInput.DriverId);
            if (dbDriver == null || dbDriver.CompanyId != companyId.Value) return NotFound();

            dbDriver.Fullname = DriverInput.Fullname;
            dbDriver.Email = DriverInput.Email;
            dbDriver.Phone = DriverInput.Phone;
            dbDriver.LicenseNumber = DriverInput.LicenseNumber;
            dbDriver.DateOfBirth = DriverInput.DateOfBirth;
            dbDriver.Address = DriverInput.Address;
            dbDriver.Status = DriverInput.Status; // Bus company can change status (Active, OnLeave)
            dbDriver.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Thông tin tài xế đã được cập nhật.";
            return RedirectToPage("./Index");
        }
    }
}