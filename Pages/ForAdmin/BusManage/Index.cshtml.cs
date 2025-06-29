using BusTicketSystem.Data;
using BusTicketSystem.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace BusTicketSystem.Pages.ForAdmin.BusManage
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;

        public IndexModel(AppDbContext context)
        {
            _context = context;
        }

        public IList<BusViewModel> Buses { get; set; } = new List<BusViewModel>();

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; } // Tìm kiếm theo biển số hoặc loại xe

        [BindProperty(SupportsGet = true)]
        public string? SortOrder { get; set; } // Sắp xếp

        public IList<BusViewModel> PendingApprovalBuses { get; set; } = new List<BusViewModel>();
        [BindProperty]
        public BusActionInputModel BusActionInput { get; set; } = new BusActionInputModel();

        public async Task OnGetAsync()
        {
            IQueryable<Bus> query = _context.Buses.Include(b => b.Company); // Include Company để lấy tên công ty

            if (!string.IsNullOrEmpty(SearchTerm))
            {
                // Tìm kiếm theo biển số hoặc loại xe (không phân biệt chữ hoa/thường)
                query = query.Where(b => b.LicensePlate.Contains(SearchTerm) || b.BusType.Contains(SearchTerm));
            }

            // Sắp xếp
            switch (SortOrder?.ToLower())
            {
                case "licenseplate_desc":
                    query = query.OrderByDescending(b => b.LicensePlate);
                    break;
                case "type":
                    query = query.OrderBy(b => b.BusType);
                    break;
                case "type_desc":
                    query = query.OrderByDescending(b => b.BusType);
                    break;
                case "capacity":
                    query = query.OrderBy(b => b.Capacity);
                    break;
                case "capacity_desc":
                    query = query.OrderByDescending(b => b.Capacity);
                    break;
                case "company":
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                    query = query.OrderBy(b => b.Company.CompanyName);
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                    break;
                case "company_desc":
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                    query = query.OrderByDescending(b => b.Company.CompanyName);
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                    break;
                case "date_asc":
                    query = query.OrderBy(b => b.CreatedAt);
                    break;
                default: // Mặc định sắp xếp theo ngày tạo giảm dần
                    query = query.OrderByDescending(b => b.CreatedAt);
                    break;
            }

            Buses = await query.Select(b => new BusViewModel(b)).ToListAsync();

            // Lấy danh sách xe đang chờ duyệt
            PendingApprovalBuses = await _context.Buses
                .Include(b => b.Company)
                .Where(b => b.Status == BusStatus.PendingApproval)
                .OrderByDescending(b => b.CreatedAt)
                .Select(b => new BusViewModel(b)).ToListAsync();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var bus = await _context.Buses.FindAsync(id);

            if (bus == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy xe cần xóa.";
                return RedirectToPage();
            }

            // TODO: Kiểm tra ràng buộc trước khi xóa (ví dụ: xe có đang được sử dụng trong chuyến đi nào không?)
            // Nếu có ràng buộc, hiển thị lỗi và không xóa.
            // Ví dụ đơn giản:
            var hasTrips = await _context.Trips.AnyAsync(t => t.BusId == id);
            if (hasTrips)
            {
                TempData["ErrorMessage"] = $"Không thể xóa xe '{bus.LicensePlate}' vì nó đang được sử dụng trong các chuyến đi.";
                return RedirectToPage();
            }

            _context.Buses.Remove(bus);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã xóa xe '{bus.LicensePlate}' thành công.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostApproveBusAsync(int id)
        {
            var busToApprove = await _context.Buses.Include(b => b.Company).FirstOrDefaultAsync(b => b.BusId == id);

            if (busToApprove == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy xe cần duyệt.";
                return RedirectToPage();
            }

            if (busToApprove.Status != BusStatus.PendingApproval)
            {
                TempData["WarningMessage"] = $"Xe '{busToApprove.LicensePlate}' không ở trạng thái chờ duyệt.";
                return RedirectToPage();
            }

            busToApprove.Status = BusStatus.Active; // Hoặc trạng thái mặc định khi được duyệt
            busToApprove.UpdatedAt = DateTime.UtcNow;
            // busToApprove.RejectionReason = null; // Xóa lý do từ chối nếu có

            // Tạo thông báo cho đối tác
            if (busToApprove.CompanyId.HasValue)
            {
                var partnerUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.CompanyId == busToApprove.CompanyId && u.Role == "partner");
                if (partnerUser != null)
                {
                    var notification = new Notification
                    {
                        RecipientUserId = partnerUser.UserId,
                        Message = $"Xe '{busToApprove.LicensePlate}' của bạn đã được duyệt và kích hoạt.",
                        Category = NotificationCategory.BusManagement,
                        TargetUrl = Url.Page("/ForPartner/BusManage/Index"),
                        IconCssClass = "bi bi-check-circle-fill"
                    };
                    _context.Notifications.Add(notification);
                }
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Đã duyệt thành công xe '{busToApprove.LicensePlate}'.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostRejectBusAsync()
        {
            if (!ModelState.IsValid)
            {
                TempData["ShowRejectBusModalOnError"] = BusActionInput.BusIdToAction.ToString();
                TempData["ActionError"] = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage ?? "Dữ liệu không hợp lệ.";
                await OnGetAsync(); // Tải lại dữ liệu
                return Page();
            }

            var busToReject = await _context.Buses.Include(b => b.Company).FirstOrDefaultAsync(b => b.BusId == BusActionInput.BusIdToAction);

            if (busToReject == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy xe cần từ chối.";
                return RedirectToPage();
            }

            if (busToReject.Status != BusStatus.PendingApproval)
            {
                TempData["WarningMessage"] = $"Xe '{busToReject.LicensePlate}' không ở trạng thái chờ duyệt.";
                return RedirectToPage();
            }

            busToReject.Status = BusStatus.Rejected;
            busToReject.RejectionReason = BusActionInput.RejectionReason; // Giả sử model Bus có thuộc tính RejectionReason
            busToReject.UpdatedAt = DateTime.UtcNow;

            // Tạo thông báo cho đối tác
            if (busToReject.CompanyId.HasValue)
            {
                var partnerUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.CompanyId == busToReject.CompanyId && u.Role == "partner");
                if (partnerUser != null)
                {
                    var notification = new Notification
                    {
                        RecipientUserId = partnerUser.UserId,
                        Message = $"Yêu cầu cho xe '{busToReject.LicensePlate}' của bạn đã bị từ chối. Lý do: {busToReject.RejectionReason}",
                        Category = NotificationCategory.BusManagement,
                        TargetUrl = Url.Page("/ForPartner/BusManage/Edit", new { id = busToReject.BusId }),
                        IconCssClass = "bi bi-x-circle-fill"
                    };
                    _context.Notifications.Add(notification);
                }
            }
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Đã từ chối yêu cầu cho xe '{busToReject.LicensePlate}'.";
            return RedirectToPage();
        }
    }

    // ViewModel để hiển thị thông tin xe, bao gồm tên công ty
    public class BusViewModel
    {
        public int BusId { get; set; }
        public string LicensePlate { get; set; } = string.Empty;
        public string BusType { get; set; } = string.Empty;
        public int? Capacity { get; set; }
        public string? CompanyName { get; set; } // Tên công ty
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public BusStatus? Status { get; set; } // Thêm thuộc tính Status

        public string StatusDisplayName => GetStatusDisplayName(Status);
        public string StatusCssClass => GetStatusCssClass(Status);

        public BusViewModel(Bus bus)
        {
            BusId = bus.BusId;
            LicensePlate = bus.LicensePlate;
            BusType = bus.BusType;
            Capacity = bus.Capacity;
            CompanyName = bus.Company?.CompanyName; // Lấy tên công ty nếu có
            CreatedAt = bus.CreatedAt;
            UpdatedAt = bus.UpdatedAt;
            Status = bus.Status; // Khởi tạo Status
        }

        private static string GetStatusDisplayName(BusStatus? status)
        {
            if (!status.HasValue) return "N/A";
            // Giả sử BusStatus enum có DisplayAttribute hoặc bạn có thể dùng switch-case
            return status.Value switch
            {
                BusStatus.Active => "Hoạt động",
                BusStatus.Inactive => "Không hoạt động",
                BusStatus.Maintenance => "Bảo trì",
                BusStatus.PendingApproval => "Chờ duyệt",
                BusStatus.Rejected => "Bị từ chối",
                _ => status.Value.ToString(),
            };
        }

        private static string GetStatusCssClass(BusStatus? status)
        {
            if (!status.HasValue) return "default";
            return status.Value.ToString().ToLower();
        }
    }

    public class BusActionInputModel
    {
        [Required]
        public int BusIdToAction { get; set; }
        [Required(ErrorMessage = "Lý do từ chối không được để trống.")]
        [StringLength(500, MinimumLength = 10, ErrorMessage = "Lý do từ chối phải có từ 10 đến 500 ký tự.")]
        public string? RejectionReason { get; set; }
    }
}