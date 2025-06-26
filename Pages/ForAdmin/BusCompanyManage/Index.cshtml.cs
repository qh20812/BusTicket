using BusTicketSystem.Data;
using BusTicketSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Linq;
using System.Threading.Tasks;

namespace BusTicketSystem.Pages.ForAdmin.BusCompanyManage
{
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;

        public IndexModel(AppDbContext context)
        {
            _context = context;
        }

        public IList<BusCompanyViewModel> PendingCompanies { get; set; } = new List<BusCompanyViewModel>();
        public IList<BusCompanyViewModel> OfficialCompanies { get; set; } = new List<BusCompanyViewModel>();

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? SortOrder { get; set; }

        [BindProperty]
        public CompanyActionInputModel CompanyActionInput { get; set; } = new CompanyActionInputModel();
        public async Task OnGetAsync()
        {
            IQueryable<BusCompany> query = _context.BusCompanies.AsQueryable();
            if (!string.IsNullOrEmpty(SearchTerm))
            {
                // query = query.Where(c => c.CompanyName.Contains(SearchTerm) || (c.Email != null && c.Email.Contains(SearchTerm)) || (c.ContactPersonName != null && c.ContactPersonName.Contains(SearchTerm)));
                string SearchTermLower = SearchTerm.ToLower();
                query = query.Where(c =>
                    (c.CompanyName != null && c.CompanyName.ToLower().Contains(SearchTermLower)) ||
                    (c.Email != null && c.Email.ToLower().Contains(SearchTermLower)) ||
                    (c.ContactPersonName != null && c.ContactPersonName.ToLower().Contains(SearchTermLower)));
            }
            switch (SortOrder?.ToLower())
            {
                case "name_asc":
                    query = query.OrderBy(c => c.CompanyName);
                    break;
                case "name_desc":
                    query = query.OrderByDescending(c => c.CompanyName);
                    break;
                case "date_asc":
                    query = query.OrderBy(c => c.CreatedAt);
                    break;
                default: // newest
                    query = query.OrderByDescending(c => c.CreatedAt);
                    break;
            }

            var allCompanies = await query.Select(c => new BusCompanyViewModel(c)).ToListAsync();

            PendingCompanies = allCompanies
                .Where(c => c.Status == BusCompanyStatus.PendingApproval || c.Status == BusCompanyStatus.UnderReview)
                .ToList();

            OfficialCompanies = allCompanies
                .Where(c => c.Status != BusCompanyStatus.PendingApproval && c.Status != BusCompanyStatus.UnderReview)
                .ToList();
        }

        // "Xem xét hồ sơ"
        public async Task<IActionResult> OnPostReviewAsync(int id)
        {
            var company = await _context.BusCompanies.FindAsync(id);
            if (company == null || company.Status != BusCompanyStatus.PendingApproval)
            {
                TempData["ErrorMessage"] = "Không tìm thấy nhà xe hoặc nhà xe không ở trạng thái chờ duyệt.";
                return RedirectToPage();
            }

            company.Status = BusCompanyStatus.UnderReview;
            company.ReviewedAt = DateTime.UtcNow;
            company.UpdatedAt = DateTime.UtcNow;

            var partnerUsersForReview = await _context.Users
                .Where(u => u.CompanyId == company.CompanyId && u.Role == "Partner").ToListAsync();
            foreach (var user in partnerUsersForReview)
            {
                user.IsActive = false;
            }
            var notification = new Notification
            {
                Message = $"Hồ sơ đăng ký của nhà xe '{company.CompanyName}' đã được chuyển sang trạng thái 'Đang xem xét'.",
                Category = NotificationCategory.BusCompany,
                TargetUrl = Url.Page("/ForAdmin/BusCompanyManage/Index", new { SearchTerm = company.CompanyName }),
                IconCssClass = "bi bi-hourglass-split"
            };
            _context.Notifications.Add(notification);

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Đã chuyển nhà xe '{company.CompanyName}' sang trạng thái 'Đang xem xét'.";
            return RedirectToPage();
        }

        // "Xác nhận (Phê duyệt)"
        public async Task<IActionResult> OnPostApproveAsync(int id)
        {
            var company = await _context.BusCompanies.FindAsync(id);
            if (company == null || company.Status != BusCompanyStatus.UnderReview)
            {
                TempData["ErrorMessage"] = "Không tìm thấy nhà xe hoặc nhà xe không ở trạng thái đang xem xét.";
                return RedirectToPage();
            }

            company.Status = BusCompanyStatus.Active; // Chính thức hoạt động
            
            company.ApprovedAt = DateTime.UtcNow;
            company.UpdatedAt = DateTime.UtcNow;

            var partnerUsersToActive = await _context.Users
                .Where(u => u.CompanyId == company.CompanyId && u.Role == "Partner").ToListAsync();
            foreach (var user in partnerUsersToActive)
            {
                user.IsActive = true;
                user.UpdatedAt = DateTime.UtcNow;
            }

            var notification = new Notification
            {
                Message = $"Nhà xe '{company.CompanyName}' đã được phê duyệt và chính thức trở thành đối tác.",
                Category = NotificationCategory.BusCompany,
                TargetUrl = Url.Page("/ForAdmin/BusCompanyManage/Index", new { SearchTerm = company.CompanyName }),
                IconCssClass = "bi bi-building-check"
            };
            _context.Notifications.Add(notification);

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Nhà xe '{company.CompanyName}' đã được phê duyệt thành công.";
            return RedirectToPage();
        }

        // Xử lý các hành động: Từ chối, Hủy hợp tác, Kích hoạt, Vô hiệu hóa
        public async Task<IActionResult> OnPostProcessActionAsync()
        {
            if (!ModelState.IsValid)
            {
                await OnGetAsync();
                TempData["ShowActionModalOnError"] = CompanyActionInput.CompanyIdToAction.ToString();
                TempData["ActionTypeOnError"] = CompanyActionInput.ActionType;
                var firstError = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage;
                TempData["ActionError"] = firstError ?? "Dữ liệu không hợp lệ.";
                return Page();
            }

            var company = await _context.BusCompanies.FindAsync(CompanyActionInput.CompanyIdToAction);
            if (company == null) return NotFound();

            string successMessage = "";
            string notificationMessage = "";
            string iconCss = "bi bi-info-circle";
            //mac dinh la false cho cac hanh dong tieu cuc
            bool targetUserIsActiveState = false;

            switch (CompanyActionInput.ActionType?.ToLower())
            {
                case "reject":
                    company.Status = BusCompanyStatus.Rejected;
                    company.RejectionReason = CompanyActionInput.Reason;
                    company.RejectedAt = DateTime.UtcNow;
                    successMessage = $"Đã từ chối hồ sơ của nhà xe '{company.CompanyName}'.";
                    notificationMessage = $"Hồ sơ nhà xe '{company.CompanyName}' đã bị từ chối. Lý do: {CompanyActionInput.Reason}";
                    iconCss = "bi bi-building-fill-x";
                    targetUserIsActiveState = false;
                    break;
                case "terminate":
                    company.Status = BusCompanyStatus.Terminated;
                    company.TerminationReason = CompanyActionInput.Reason;
                    company.TerminatedAt = DateTime.UtcNow;
                    successMessage = $"Đã hủy hợp tác với nhà xe '{company.CompanyName}'.";
                    notificationMessage = $"Nhà xe '{company.CompanyName}' đã bị hủy hợp tác. Lý do: {CompanyActionInput.Reason}";
                    iconCss = "bi bi-building-dash";
                    targetUserIsActiveState = false;
                    break;
                case "deactivate":
                    company.Status = BusCompanyStatus.Inactive;
                    successMessage = $"Đã vô hiệu hóa nhà xe '{company.CompanyName}'.";
                    notificationMessage = $"Nhà xe '{company.CompanyName}' đã được chuyển sang trạng thái không hoạt động.";
                    iconCss = "bi bi-building-slash";
                    targetUserIsActiveState = false;
                    break;
                case "activate":
                    company.Status = BusCompanyStatus.Active;
                    successMessage = $"Đã kích hoạt lại nhà xe '{company.CompanyName}'.";
                    notificationMessage = $"Nhà xe '{company.CompanyName}' đã được kích hoạt trở lại.";
                    iconCss = "bi bi-building-up";
                    targetUserIsActiveState = true;
                    break;
                case "repartner": // Hợp tác lại
                    company.Status = BusCompanyStatus.Active;
                    company.ApprovedAt = DateTime.UtcNow;
                    company.TerminationReason = null;
                    company.TerminatedAt = null;
                    company.RejectionReason = null; // Clear previous rejection info as well
                    company.RejectedAt = null;
                    successMessage = $"Đã hợp tác lại với nhà xe '{company.CompanyName}'.";
                    notificationMessage = $"Nhà xe '{company.CompanyName}' đã được kích hoạt và hợp tác trở lại.";
                    iconCss = "bi bi-arrow-clockwise";
                    targetUserIsActiveState = true;
                    break;
                default:
                    TempData["ErrorMessage"] = "Hành động không hợp lệ.";
                    return RedirectToPage();
            }
            company.UpdatedAt = DateTime.UtcNow;

            if (company.CompanyId > 0)
            {
                var partnerUsersToUpdate = await _context.Users
                    .Where(u => u.CompanyId == company.CompanyId && u.Role == "Partner")
                    .ToListAsync();
                foreach (var user in partnerUsersToUpdate)
                {
                    user.IsActive = targetUserIsActiveState;
                    user.UpdatedAt = DateTime.UtcNow;
                }
}

            var notification = new Notification
            {
                Message = notificationMessage,
                Category = NotificationCategory.BusCompany,
                TargetUrl = Url.Page("/ForAdmin/BusCompanyManage/Index", new { SearchTerm = company.CompanyName }),
                IconCssClass = iconCss
            };
            _context.Notifications.Add(notification);

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = successMessage;
            return RedirectToPage();
        }
    }

    public class BusCompanyViewModel
    {
        public int CompanyId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? ContactPersonName { get; set; }
        public BusCompanyStatus Status { get; set; }
        public DateTime CreatedAt { get; set; } // Ngày đăng ký
        public DateTime? ApprovedAt { get; set; } // Ngày duyệt
        public string? RejectionReason { get; set; }
        public string? TerminationReason { get; set; }

        public string StatusDisplayName => Status.GetType().GetField(Status.ToString())?.GetCustomAttribute<System.ComponentModel.DataAnnotations.DisplayAttribute>()?.GetName() ?? Status.ToString();

        public BusCompanyViewModel(BusCompany company)
        {
            CompanyId = company.CompanyId;
            CompanyName = company.CompanyName??string.Empty;
            Email = company.Email;
            Phone = company.Phone;
            ContactPersonName = company.ContactPersonName;
            Status = company.Status;
            CreatedAt = company.CreatedAt;
            ApprovedAt = company.ApprovedAt;
            RejectionReason = company.RejectionReason;
            TerminationReason = company.TerminationReason;
        }
    }

    public class CompanyActionInputModel
    {
        [Required]
        public int CompanyIdToAction { get; set; }

        [StringLength(500, ErrorMessage = "Lý do không được vượt quá 500 ký tự.")]
        // Lý do không bắt buộc cho tất cả hành động, nhưng bắt buộc cho Reject và Terminate
        public string? Reason { get; set; }

        [Required]
        public string ActionType { get; set; } = string.Empty; // "Reject", "Terminate", "Deactivate", "Activate", "Repartner"
    }
}