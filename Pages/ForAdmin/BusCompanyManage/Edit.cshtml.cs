using BusTicketSystem.Data;
using BusTicketSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using System.Reflection;

namespace BusTicketSystem.Pages.ForAdmin.BusCompanyManage
{
    public class EditModel : PageModel
    {
        private readonly AppDbContext _context;

        public EditModel(AppDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public CompanyInputModel CompanyInput { get; set; } = new CompanyInputModel();

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) // Thêm mới
            {
                ViewData["Title"] = "Thêm mới Nhà xe";
                CompanyInput.Status = BusCompanyStatus.Active; // Mặc định là Active khi admin thêm
                return Page();
            }

            // Chỉnh sửa
            ViewData["Title"] = "Chỉnh sửa Nhà xe";
            var company = await _context.BusCompanies.FindAsync(id.Value);

            if (company == null)
            {
                return NotFound();
            }

            CompanyInput = new CompanyInputModel
            {
                CompanyId = company.CompanyId,
                CompanyName = company.CompanyName,
                Address = company.Address ?? string.Empty,
                Phone = company.Phone ?? string.Empty,
                Email = company.Email ?? string.Empty,
                Description = company.Description ?? string.Empty,
                ContactPersonName = company.ContactPersonName,
                ContactPersonEmail = company.ContactPersonEmail,
                ContactPersonPhone = company.ContactPersonPhone,
                Status = company.Status,
                CreatedAt = company.CreatedAt,
                ApprovedAt = company.ApprovedAt,
                ReviewedAt = company.ReviewedAt,
                RejectedAt = company.RejectedAt,
                RejectionReason = company.RejectionReason,
                TerminatedAt = company.TerminatedAt,
                TerminationReason = company.TerminationReason
            };

            if (company.Status == BusCompanyStatus.Rejected || company.Status == BusCompanyStatus.Terminated)
            {
                TempData["InfoMessage"] = $"Nhà xe '{company.CompanyName}' đang ở trạng thái '{company.Status.GetType().GetField(company.Status.ToString())?.GetCustomAttribute<DisplayAttribute>()?.GetName()}'. Một số thông tin có thể không được phép chỉnh sửa.";
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                ViewData["Title"] = CompanyInput.CompanyId == 0 ? "Thêm mới Nhà xe" : "Chỉnh sửa Nhà xe";
                return Page();
            }

            BusCompany companyToUpdate;
            string successMessage;
            string notificationMessage;

            if (CompanyInput.CompanyId == 0) // Thêm mới
            {
                // Kiểm tra trùng tên hoặc email
                if (await _context.BusCompanies.AnyAsync(c => c.CompanyName == CompanyInput.CompanyName))
                {
                    ModelState.AddModelError("CompanyInput.CompanyName", "Tên công ty đã tồn tại.");
                    ViewData["Title"] = "Thêm mới Nhà xe";
                    return Page();
                }
                if (CompanyInput.Email != null && await _context.BusCompanies.AnyAsync(c => c.Email == CompanyInput.Email))
                {
                    ModelState.AddModelError("CompanyInput.Email", "Email công ty đã tồn tại.");
                    ViewData["Title"] = "Thêm mới Nhà xe";
                    return Page();
                }

                companyToUpdate = new BusCompany { CreatedAt = DateTime.UtcNow };
                _context.BusCompanies.Add(companyToUpdate);
                successMessage = $"Đã thêm mới nhà xe '{CompanyInput.CompanyName}' thành công.";
                notificationMessage = $"Nhà xe '{CompanyInput.CompanyName}' vừa được thêm mới vào hệ thống bởi quản trị viên.";
                companyToUpdate.ApprovedAt = CompanyInput.Status == BusCompanyStatus.Active ? DateTime.UtcNow : (DateTime?)null;
                companyToUpdate.ReviewedAt = (CompanyInput.Status == BusCompanyStatus.Active || CompanyInput.Status == BusCompanyStatus.UnderReview) ? DateTime.UtcNow : (DateTime?)null;

                // Map all fields from CompanyInput for new company
                companyToUpdate.CompanyName = CompanyInput.CompanyName;
                companyToUpdate.Address = CompanyInput.Address;
                companyToUpdate.Phone = CompanyInput.Phone;
                companyToUpdate.Email = CompanyInput.Email;
                companyToUpdate.Description = CompanyInput.Description;
                companyToUpdate.ContactPersonName = CompanyInput.ContactPersonName;
                companyToUpdate.ContactPersonEmail = CompanyInput.ContactPersonEmail;
                companyToUpdate.ContactPersonPhone = CompanyInput.ContactPersonPhone;
                companyToUpdate.Status = CompanyInput.Status;
            }
            else // Chỉnh sửa
            {
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
                companyToUpdate = await _context.BusCompanies.FindAsync(CompanyInput.CompanyId);
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
                if (companyToUpdate == null) return NotFound();

                // Uniqueness checks removed for edit as fields are readonly

                successMessage = $"Đã cập nhật thông tin nhà xe '{CompanyInput.CompanyName}' thành công.";
                notificationMessage = $"Thông tin nhà xe '{CompanyInput.CompanyName}' vừa được cập nhật.";

                // Preserve read-only fields from CompanyInput (which holds values from hidden fields)
                companyToUpdate.CompanyName = CompanyInput.CompanyName;
                companyToUpdate.Address = CompanyInput.Address;
                companyToUpdate.Phone = CompanyInput.Phone;
                companyToUpdate.Email = CompanyInput.Email;
                companyToUpdate.Description = CompanyInput.Description;
                companyToUpdate.ContactPersonName = CompanyInput.ContactPersonName;
                companyToUpdate.ContactPersonEmail = CompanyInput.ContactPersonEmail;
                companyToUpdate.ContactPersonPhone = CompanyInput.ContactPersonPhone;
                companyToUpdate.CreatedAt = CompanyInput.CreatedAt; // Preserve CreatedAt

                var oldStatus = companyToUpdate.Status;
                var newStatus = CompanyInput.Status;

                if (oldStatus != newStatus)
                {
                    companyToUpdate.Status = newStatus;
                    if (newStatus == BusCompanyStatus.Active)
                    {
                        companyToUpdate.ApprovedAt = companyToUpdate.ApprovedAt ?? DateTime.UtcNow;
                        companyToUpdate.ReviewedAt = companyToUpdate.ReviewedAt ?? DateTime.UtcNow;
                        companyToUpdate.RejectionReason = null; companyToUpdate.RejectedAt = null;
                        companyToUpdate.TerminationReason = null; companyToUpdate.TerminatedAt = null;
                    }
                    else if (newStatus == BusCompanyStatus.PendingApproval)
                    {
                        companyToUpdate.ApprovedAt = null; companyToUpdate.ReviewedAt = null;
                        companyToUpdate.RejectionReason = null; companyToUpdate.RejectedAt = null;
                        companyToUpdate.TerminationReason = null; companyToUpdate.TerminatedAt = null;
                    }
                    else if (newStatus == BusCompanyStatus.UnderReview)
                    {
                        companyToUpdate.ReviewedAt = companyToUpdate.ReviewedAt ?? DateTime.UtcNow;
                        companyToUpdate.ApprovedAt = null;
                        companyToUpdate.RejectionReason = null; companyToUpdate.RejectedAt = null;
                        companyToUpdate.TerminationReason = null; companyToUpdate.TerminatedAt = null;
                    }
                    // Other statuses like Inactive, Rejected, Terminated are typically set via Index page actions with reasons.
                }
            }
            companyToUpdate.UpdatedAt = DateTime.UtcNow;

            var notification = new Notification
            {
                Message = notificationMessage, Category = NotificationCategory.BusCompany,
                TargetUrl = Url.Page("./Index", new { SearchTerm = companyToUpdate.CompanyName }), IconCssClass = "bi bi-buildings"
            };
            _context.Notifications.Add(notification);

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = successMessage;
            return RedirectToPage("./Index");
        }
    }

    public class CompanyInputModel
    {
        public int CompanyId { get; set; }

        [Required(ErrorMessage = "Tên công ty không được để trống.")]
        [StringLength(100)] public string CompanyName { get; set; } = string.Empty;
        
        // Address can be optional for some initial states, adjust Required if needed
        public string Address { get; set; } = string.Empty;

        [Phone][StringLength(20)] public string Phone { get; set; } = string.Empty;

        [EmailAddress][StringLength(100)] public string Email { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Display(Name = "Tên người liên hệ")] [StringLength(100)] public string? ContactPersonName { get; set; }
        [Display(Name = "Email người liên hệ")] [EmailAddress] [StringLength(100)] public string? ContactPersonEmail { get; set; }
        [Display(Name = "SĐT người liên hệ")] [Phone] [StringLength(20)] public string? ContactPersonPhone { get; set; }

        [Required(ErrorMessage = "Trạng thái không được để trống.")]
        public BusCompanyStatus Status { get; set; }

        // Fields to preserve and display
        public DateTime CreatedAt { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public DateTime? RejectedAt { get; set; }
        public string? RejectionReason { get; set; }
        public DateTime? TerminatedAt { get; set; }
        public string? TerminationReason { get; set; }
    }
}