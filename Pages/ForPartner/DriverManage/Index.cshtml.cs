using BusTicketSystem.Data;
using BusTicketSystem.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims; // Required for User.FindFirstValue

namespace BusTicketSystem.Pages.ForPartner.DriverManage
{
    // [Authorize(Policy = "BusCompanyOnly")] // Add appropriate authorization
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;

        public IndexModel(AppDbContext context)
        {
            _context = context;
        }

        public IList<DriverViewModel> CompanyDrivers { get;set; } = new List<DriverViewModel>();

        [BindProperty(SupportsGet = true)]
        public string? SearchName { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? SortName { get; set; } // az, za

        [BindProperty]
        public DriverActionInputModel DriverActionInput { get; set; } = new DriverActionInputModel();

        private int? GetCurrentBusCompanyId()
        {
            var companyIdClaim = User.FindFirstValue("CompanyId"); // Example: if CompanyId is a claim
            if (int.TryParse(companyIdClaim, out int companyId))
            {
                return companyId;
            }
            // Fallback or error handling if CompanyId cannot be determined.
            // For demonstration, if you have a user linked to a company:
            // var user = await _context.Users.Include(u => u.BusCompany).FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);
            // return user?.BusCompanyId;
            return null;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var companyId = GetCurrentBusCompanyId();
            if (companyId == null)
            {
                TempData["ErrorMessage"] = "Không thể xác định thông tin nhà xe. Vui lòng đăng nhập lại.";
                return RedirectToPage("/Account/Login/Login"); 
            }

            IQueryable<Driver> query = _context.Drivers
                                            .Where(d => d.CompanyId == companyId.Value);

            if (!string.IsNullOrEmpty(SearchName))
            {
                query = query.Where(d => d.Fullname != null && d.Fullname.Contains(SearchName));
            }

            switch (SortName?.ToLower())
            {
                case "az":
                    query = query.OrderBy(d => d.Fullname);
                    break;
                case "za":
                    query = query.OrderByDescending(d => d.Fullname);
                    break;
                default:
                    query = query.OrderByDescending(d => d.JoinedDate).ThenByDescending(d => d.CreatedAt);
                    break;
            }

            CompanyDrivers = await query.Select(d => new DriverViewModel(d)).ToListAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostProcessActionAsync()
        {
            var companyId = GetCurrentBusCompanyId();
            if (companyId == null) return Forbid();

            if (!ModelState.IsValid)
            {
                await OnGetAsync(); // Reload data
                TempData["ShowActionModalOnError"] = DriverActionInput.DriverIdToAction.ToString();
                TempData["ActionTypeOnError"] = DriverActionInput.ActionType;
                TempData["ActionError"] = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage ?? "Dữ liệu không hợp lệ.";
                return Page();
            }

            var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.DriverId == DriverActionInput.DriverIdToAction && d.CompanyId == companyId.Value);
            if (driver == null) return NotFound("Không tìm thấy tài xế hoặc tài xế không thuộc nhà xe của bạn.");

            string successMessage;
            string notificationMessage;

            // Bus companies might only be able to "Terminate" (sa thải) or "Resign" (cho nghỉ việc)
            // "Reject" is usually for applications.
            if (DriverActionInput.ActionType == "Terminate")
            {
                // Logic xóa tài xế
                // Cẩn thận: Kiểm tra các ràng buộc trước khi xóa
                var tripsAssociated = await _context.Trips.AnyAsync(t => t.DriverId == driver.DriverId && t.Status == TripStatus.Scheduled);
                if (tripsAssociated)
                {
                    await OnGetAsync(); // Tải lại dữ liệu
                    TempData["ShowActionModalOnError"] = DriverActionInput.DriverIdToAction.ToString();
                    TempData["ActionTypeOnError"] = DriverActionInput.ActionType;
                    TempData["ActionError"] = $"Không thể xóa tài xế '{driver.Fullname}' vì tài xế này đang được gán cho các chuyến đi chưa hoàn thành. Vui lòng hủy hoặc hoàn thành các chuyến đi đó trước.";
                    return Page();
                }

                _context.Drivers.Remove(driver);
                // Không cần cập nhật Status, TerminationDate, TerminationReason nữa vì tài xế sẽ bị xóa

                successMessage = $"Đã xóa thông tin tài xế '{driver.Fullname}' khỏi hệ thống theo yêu cầu cho nghỉ việc.";
                // Thông báo cho admin có thể cần điều chỉnh để phản ánh việc xóa
                notificationMessage = $"Tài xế '{driver.Fullname}' (ID: {driver.DriverId}) thuộc nhà xe của bạn đã được xóa khỏi hệ thống theo yêu cầu cho nghỉ việc. Lý do: {DriverActionInput.Reason}";
            }
            else
            {
                return BadRequest("Hành động không hợp lệ.");
            }

            driver.UpdatedAt = DateTime.UtcNow;
            // Tạo thông báo cho Admin hoặc các bên liên quan
            var notification = new BusTicketSystem.Models.Notification
            {
                Message = notificationMessage,
                Category = NotificationCategory.Driver,
                TargetUrl = Url.Page("/ForAdmin/DriverManage/Index", new { SearchName = driver.Fullname }), // Admin might want to see this
                IconCssClass = "bi bi-person-dash-fill",
                RecipientUserId = null // Or AdminRecipientId
            };
            _context.Notifications.Add(notification);

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = successMessage;
            return RedirectToPage();
        }
    }
} 