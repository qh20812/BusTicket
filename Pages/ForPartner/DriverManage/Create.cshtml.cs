using BusTicketSystem.Data;
using BusTicketSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Threading.Tasks;

namespace BusTicketSystem.Pages.ForPartner.DriverManage
{
    // [Authorize(Policy = "PartnerOnly")]
    public class CreateModel : PageModel
    {
        private readonly AppDbContext _context;

        public CreateModel(AppDbContext context)
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

        public IActionResult OnGet()
        {
            ViewData["Title"] = "Thêm Tài xế mới cho Nhà xe";
            DriverInput.Status = DriverStatus.Active;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var companyId = GetCurrentBusCompanyId();
            if (companyId == null)
            {
                ModelState.AddModelError(string.Empty, "Không thể xác định nhà xe của bạn.");
                return Page();
            }

            if (await _context.Drivers.AnyAsync(d => d.LicenseNumber == DriverInput.LicenseNumber))
            {
                ModelState.AddModelError("DriverInput.LicenseNumber", "Số bằng lái này đã tồn tại trong hệ thống.");
            }

            if (!ModelState.IsValid)
            {
                ViewData["Title"] = "Thêm Tài xế mới cho Nhà xe";
                return Page();
            }

            var newDriver = new Driver
            {
                Fullname = DriverInput.Fullname,
                Email = DriverInput.Email,
                Phone = DriverInput.Phone,
                LicenseNumber = DriverInput.LicenseNumber,
                DateOfBirth = DriverInput.DateOfBirth,
                Address = DriverInput.Address,
                Status = DriverInput.Status,
                CompanyId = companyId.Value,
                JoinedDate = (DriverInput.Status == DriverStatus.Active) ? DateTime.UtcNow.Date : (DateTime?)null,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Drivers.Add(newDriver);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã thêm tài xế '{newDriver.Fullname}' vào nhà xe của bạn.";
            return RedirectToPage("./Index");
        }
    }
}