using BusTicketSystem.Data;
using BusTicketSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Threading.Tasks;
using System.Security.Claims;
using BusTicketSystem.Pages.ForAdmin.RouteManage;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.AspNetCore.Authorization;

namespace BusTicketSystem.Pages.ForPartner.RouteManage
{
    [Authorize(Roles = "Partner")]
    public class EditModel : PageModel
    {
        private readonly AppDbContext _db;
        public EditModel(AppDbContext db)
        {
            _db = db;
        }
        [BindProperty]
        public RouteInputModel RouteInput { get; set; } = new RouteInputModel();
        public string ErrorMessage { get; set; }
        private async Task<int?> GetCurrentPartnerCompanyIdAsync()
        {
            await Task.CompletedTask;
            return 1;
        }
        public async Task<IActionResult> OnGetAsync(int? id)
        {
            var partnerCompanyId = await GetCurrentPartnerCompanyIdAsync();
            if (!partnerCompanyId.HasValue)
            {
                return Forbid();
            }
            if (id == null)
            {
                ViewData["Title"] = "Đề xuất Lộ trình mới";
                RouteInput = new RouteInputModel
                {
                    ProposedByCompanyId = partnerCompanyId.Value,
                    Status = RouteStatus.PendingApproval
                };
            }
            else
            {
                ViewData["Title"] = "Chỉnh sửa Lộ trình Đề xuất";
                var route = await _db.Routes.FindAsync(id.Value);
                if (route == null || route.ProposedByCompanyId != partnerCompanyId.Value)
                {
                    return NotFound("Không tìm thấy lộ trình hoặc bạn không có quyền chỉnh sửa lộ trình này.");
                }
                if (route.Status != RouteStatus.PendingApproval && route.Status != RouteStatus.Rejected)
                {
                    TempData["ErrorMessage"] = "Bạn chỉ có thể chỉnh sửa lộ trình đang chờ duyệt hoặc bị từ chối.";
                    return RedirectToPage("./Index");
                }
                RouteInput = new RouteInputModel
                {
                    RouteId = route.RouteId,
                    Departure = route.Departure,
                    Destination = route.Destination,
                    OriginAddress = route.OriginAddress,
                    DestinationAddress = route.DestinationAddress,
                    OriginCoordinates = route.OriginCoordinates,
                    DestinationCoordinates = route.DestinationCoordinates,
                    Distance = route.Distance,
                    EstimatedDuration = route.EstimatedDuration?.ToString(@"hh\:mm"),
                    Status = RouteStatus.PendingApproval,
                    ProposedByCompanyId = route.ProposedByCompanyId.Value
                };
            }
            return Page();
        }
        public async Task<IActionResult> OnPostAsync()
        {
            var partnerCompanyId = await GetCurrentPartnerCompanyIdAsync();
            if (!partnerCompanyId.HasValue)
            {
                return Forbid();
            }
            RouteInput.ProposedByCompanyId = partnerCompanyId.Value;
            RouteInput.Status = RouteStatus.PendingApproval;
            if (!ModelState.IsValid)
            {
                ViewData["Title"] = RouteInput.RouteId == 0 ? "Đề xuất Lộ trình mới" : "Chỉnh sửa Lộ trình Đề xuất";
                return Page();
            }
            TimeSpan? estimatedDurationTimeSpan = null;
            if (!string.IsNullOrEmpty(RouteInput.EstimatedDuration))
            {
                if (!TimeSpan.TryParseExact(RouteInput.EstimatedDuration, "hh\\:mm", CultureInfo.InvariantCulture, out TimeSpan parsedDuration))
                {
                    ModelState.AddModelError("RouteInput.EstimatedDuration", "Định dạng Thời gian di chuyển ước tính không hợp lệ. Sử dụng HH:mm.");
                    ViewData["Title"] = RouteInput.RouteId == 0 ? "Đề xuất Lộ trình mới" : "Chỉnh sửa Lộ trình Đề xuất";
                    return Page();
                }
                estimatedDurationTimeSpan = parsedDuration;
            }
            if (RouteInput.RouteId == 0)
            {
                bool routeExists = await _db.Routes.AnyAsync(r => r.Departure == RouteInput.Departure && r.Destination == RouteInput.Destination);
                if (routeExists)
                {
                    ModelState.AddModelError(string.Empty, $"Lộ trình từ '{RouteInput.Departure}' đến '{RouteInput.Destination}' đã tồn tại trong hệ thống.");
                    ViewData["Title"] = "Đề xuất Lộ trình mới";
                    return Page();
                }
                var newRoute = new Models.Route
                {
                    Departure = RouteInput.Departure,
                    Destination = RouteInput.Destination,
                    OriginAddress = RouteInput.OriginAddress,
                    DestinationAddress = RouteInput.DestinationAddress,
                    OriginCoordinates = RouteInput.OriginCoordinates,
                    DestinationCoordinates = RouteInput.DestinationCoordinates,
                    Distance = RouteInput.Distance,
                    EstimatedDuration = estimatedDurationTimeSpan,
                    Status = RouteStatus.PendingApproval,
                    ProposedByCompanyId = partnerCompanyId.Value,
                    Trips = new List<Models.Trip>(),
                    Promotions = new List<Models.Promotion>(),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _db.Routes.Add(newRoute);
                TempData["SuccessMessage"] = "Đã gửi đề xuất lộ trình mới thành công. Vui lòng chờ quản trị viên duyệt.";
            }
            else
            {
                var routeToUpdate = await _db.Routes.FindAsync(RouteInput.RouteId);
                if (routeToUpdate == null || routeToUpdate.ProposedByCompanyId != partnerCompanyId.Value)
                {
                    return NotFound("Lộ trình không tồn tại hoặc bạn không có quyền.");
                }
                if (routeToUpdate.Status != RouteStatus.PendingApproval && routeToUpdate.Status != RouteStatus.Rejected)
                {
                    TempData["ErrorMessage"] = "Bạn chỉ có thể chỉnh sửa lộ trình đang chờ duyệt hoặc bị từ chối để gửi lại.";
                    return RedirectToPage("./Index");
                }
                routeToUpdate.Departure = RouteInput.Departure;
                routeToUpdate.Destination = RouteInput.Destination;
                routeToUpdate.OriginAddress = RouteInput.OriginAddress;
                routeToUpdate.DestinationAddress = RouteInput.DestinationAddress;
                routeToUpdate.OriginCoordinates = RouteInput.OriginCoordinates;
                routeToUpdate.DestinationCoordinates = RouteInput.DestinationCoordinates;
                routeToUpdate.Distance = RouteInput.Distance;
                routeToUpdate.EstimatedDuration = estimatedDurationTimeSpan;
                routeToUpdate.Status = RouteStatus.PendingApproval;
                routeToUpdate.UpdatedAt = DateTime.UtcNow;

                _db.Attach(routeToUpdate).State = EntityState.Modified;
                TempData["SuccessMessage"] = "Đã cập nhật và gửi lại đề xuất lộ trình. Vui lòng chờ quản trị viên duyệt.";
            }
            await _db.SaveChangesAsync();
            return RedirectToPage("./Index");
        }
    }
    public class RouteInputModel
    {
        public int RouteId { get; set; }
        [Required(ErrorMessage = "Điểm đi không được để trống.")]
        [StringLength(100)]
        [Display(Name = "Điểm đi")]
        public string Departure { get; set; } = string.Empty;
        [Required(ErrorMessage = "Điểm đến không được để trống.")]
        [StringLength(100)]
        [Display(Name = "Điểm đến")]
        public string Destination { get; set; } = string.Empty;
        [StringLength(255)]
        [Display(Name = "Địa chỉ chi tiết điểm đi")]
        public string? OriginAddress { get; set; }
        [StringLength(255)]
        [Display(Name = "Địa chỉ chi tiết điểm đến")]
        public string? DestinationAddress { get; set; }
        [StringLength(50)]
        [RegularExpression(@"^-?\d+(\.\d+)?,-?\d+(\.\d+)?$", ErrorMessage = "Định dạng tọa độ không hợp lệ (ví dụ: 10.123,-70.456).")]
        [Display(Name = "Tọa độ điểm đi (lat,lon)")]
        public string? OriginCoordinates { get; set; }
        [StringLength(50)]
        [RegularExpression(@"^-?\d+(\.\d+)?,-?\d+(\.\d+)?$", ErrorMessage = "Định dạng tọa độ không hợp lệ (ví dụ: 10.123,-70.456).")]
        [Display(Name = "Tọa độ điểm đến (lat,lon)")]
        public string? DestinationCoordinates { get; set; }
        [Range(0, double.MaxValue, ErrorMessage = "Khoảng cách phải là số không âm.")]
        [Display(Name = "Khoảng cách (km)")]
        public decimal? Distance { get; set; }
        [RegularExpression(@"^([0-1]?[0-9]|2[0-3]):[0-5][0-9]$", ErrorMessage = "Thời gian ước tính phải có định dạng HH:mm (ví dụ: 02:30).")]
        [Display(Name = "Thời gian di chuyển ước tính (HH:mm)")]
        public string? EstimatedDuration { get; set; }
        public RouteStatus Status { get; set; }
        public int ProposedByCompanyId { get; set; }
    }
}