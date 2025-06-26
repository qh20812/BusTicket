using BusTicketSystem.Data;
using BusTicketSystem.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BusTicketSystem.Pages.ForAdmin.DriverManage
{
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;

        public IndexModel(AppDbContext context)
        {
            _context = context;
        }

        public IList<DriverViewModel> PendingApplications { get;set; } = new List<DriverViewModel>();
        public IList<DriverViewModel> OfficialDrivers { get;set; } = new List<DriverViewModel>();

        [BindProperty(SupportsGet = true)]
        public string? SearchName { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? SortName { get; set; }

        [BindProperty]
        public DriverActionInputModel DriverActionInput { get; set; } = new DriverActionInputModel();

        public async Task OnGetAsync()
        {
            IQueryable<Driver> query = _context.Drivers.Include(d => d.Company);
            

            if (!string.IsNullOrEmpty(SearchName))
            {
                query = query.Where(d => d.Fullname != null && d.Fullname.Contains(SearchName));
            }

            // Sắp xếp chung trước khi phân loại
            switch (SortName?.ToLower())
            {
                case "az":
                    query = query.OrderBy(d => d.Fullname);
                    break;
                case "za":
                    query = query.OrderByDescending(d => d.Fullname);
                    break;
                default:
                    query = query.OrderByDescending(d => d.CreatedAt); // Mặc định sắp xếp theo ngày tạo/nộp hồ sơ
                    break;
            }

            var allDrivers = await query.ToListAsync();

            PendingApplications = allDrivers
                .Where(d => d.Status == DriverStatus.PendingApproval || d.Status == DriverStatus.UnderReview)
                .Select(d => new DriverViewModel(d)).ToList();

            OfficialDrivers = allDrivers
                .Where(d => d.Status != DriverStatus.PendingApproval && d.Status != DriverStatus.UnderReview)
                .Select(d => new DriverViewModel(d)).ToList();
            
        }

        // "Nhận hồ sơ"
        public async Task<IActionResult> OnPostReviewAsync(int id)
        {
            var driver = await _context.Drivers.FindAsync(id);
            if (driver == null || driver.Status != DriverStatus.PendingApproval) return NotFound();

            driver.Status = DriverStatus.UnderReview;
            driver.UpdatedAt = DateTime.UtcNow;
            var notification = new Notification
            {
                Message = $"Hồ sơ của tài xế '{driver.Fullname}' đã được chuyển sang trạng thái đang xem xét.",
                Category = NotificationCategory.Driver,
                TargetUrl = Url.Page("/ForAdmin/DriverManage/Index", new { SearchName = driver.Fullname }),
                IconCssClass = "bi bi-person-lines-fill",
                RecipientUserId = null
            };
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Đã nhận hồ sơ của '{driver.Fullname}'. Chuyển sang trạng thái đang xem xét.";
            return RedirectToPage();
        }

        // "Tuyển dụng"
        public async Task<IActionResult> OnPostApproveAsync(int id)
        {
            var driver = await _context.Drivers.FindAsync(id);
            if (driver == null || driver.Status != DriverStatus.UnderReview) return NotFound();

            driver.Status = DriverStatus.Active;
            driver.JoinedDate = DateTime.UtcNow.Date; // Cập nhật ngày tham gia chính thức
            driver.UpdatedAt = DateTime.UtcNow;
            var notification = new Notification
            {
                Message = $"Tài xế '{driver.Fullname}' đã được tuyển dụng chính thức.",
                Category = NotificationCategory.Driver,
                TargetUrl = Url.Page("/ForAdmin/DriverManage/Index", new { SearchName = driver.Fullname }),
                IconCssClass = "bi bi-person-check-fill",
                RecipientUserId = null
            };
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Đã tuyển dụng '{driver.Fullname}' vào danh sách tài xế chính thức.";
            return RedirectToPage();
        }

        // "Từ chối hồ sơ" hoặc "Sa thải"
        public async Task<IActionResult> OnPostProcessActionAsync()
        {
            if (!ModelState.IsValid)
            {
                await OnGetAsync();
                TempData["ShowActionModalOnError"] = DriverActionInput.DriverIdToAction.ToString();
                TempData["ActionTypeOnError"] = DriverActionInput.ActionType;
                var firstError = ModelState.Values.SelectMany(v => v.Errors).Select(e=>e.ErrorMessage);
                TempData["ActionError"] = string.Join("",firstError);
                return Page();
            }

            var driver = await _context.Drivers.FindAsync(DriverActionInput.DriverIdToAction);
            if (driver == null) return NotFound();

            string successMessage;
            string notificationMessage;
            string notificationIcon;
            if (DriverActionInput.ActionType == "Reject")
            {
                driver.Status = DriverStatus.Rejected;
                successMessage = $"Đã từ chối hồ sơ của '{driver.Fullname}'.";
                notificationMessage = $"Hồ sơ ứng tuyển của '{driver.Fullname}' đã bị từ chối. Lý do: {DriverActionInput.Reason}";
                notificationIcon = "bi bi-person-x-fill";
            }
            else if (DriverActionInput.ActionType == "Terminate")
            {
                driver.Status = DriverStatus.Terminated;
                driver.TerminationDate = DateTime.UtcNow.Date;
                successMessage = $"Đã sa thải tài xế '{driver.Fullname}'.";
                notificationMessage = $"Tài xế '{driver.Fullname}' đã bị sa thải. Lý do: {DriverActionInput.Reason}";
                notificationIcon = "bi bi-person-dash-fill";
            }
            else return BadRequest("Hành động không hợp lệ.");

            driver.TerminationReason = DriverActionInput.Reason; // Dùng chung cho cả từ chối và sa thải
            driver.UpdatedAt = DateTime.UtcNow;
            var notification = new Notification
            {
                Message = notificationMessage,
                Category = NotificationCategory.Driver,
                TargetUrl = Url.Page("/ForAdmin/DriverManage/Index", new { SearchName = driver.Fullname }),
                IconCssClass = notificationIcon,
                RecipientUserId = null
            };
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = successMessage;
            return RedirectToPage();
        }
    }
}