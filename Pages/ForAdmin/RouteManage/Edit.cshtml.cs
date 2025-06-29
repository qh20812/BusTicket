using System.ComponentModel.DataAnnotations;
using System.Globalization;
using BusTicketSystem.Data;
using BusTicketSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace BusTicketSystem.Pages.ForAdmin.RouteManage
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
        public RouteInputModel RouteInput { get; set; } = new RouteInputModel();
        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                ViewData["Title"] = "Thêm mới lộ trình";
                RouteInput = new RouteInputModel();
            }
            else
            {
                ViewData["Title"] = "Chỉnh sửa lộ trình";
                var route = await _context.Routes.FindAsync(id.Value);
                if (route == null)
                {
                    return NotFound();
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
                    Status = route.Status,
                    ProposedByCompanyId = route.ProposedByCompanyId
                };
            }
            return Page();
        }
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                ViewData["Title"] = RouteInput.RouteId == 0 ? "Thêm mới lộ trình" : "Chỉnh sửa lộ trình";
                return Page();
            }
            TimeSpan? estimatedDurationTimeSpan = null;
            if (!string.IsNullOrEmpty(RouteInput.EstimatedDuration))
            {
                if (TimeSpan.TryParseExact(RouteInput.EstimatedDuration, "hh\\:mm", CultureInfo.InvariantCulture, out TimeSpan parsedDuration))
                {
                    estimatedDurationTimeSpan = parsedDuration;
                }
                else
                {
                    ModelState.AddModelError("RouteInput.EstimatedDuration", "Định dạng Thời gian di chuyển ước tính không hợp lệ. Sử dụng HH:mm.");
                    ViewData["Title"] = RouteInput.RouteId == 0 ? "Thêm mới lộ trình" : "Chỉnh sửa lộ trình";
                    return Page();
                }
            }
            if (RouteInput.RouteId == 0)
            {
                bool routeExists = await _context.Routes.AnyAsync(r => r.Departure == RouteInput.Departure && r.Destination == RouteInput.Destination);
                if (routeExists)
                {
                    ModelState.AddModelError(string.Empty, $"Lộ trình từ '{RouteInput.Departure}' đến '{RouteInput.Destination}' đã tồn tại.");
                    ViewData["Title"] = "Thêm mới Lộ trình";
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
                    Status = RouteInput.Status,
                    Trips = new List<Trip>(),
                    Promotions = new List<Promotion>(),
                    ProposedByCompanyId = RouteInput.ProposedByCompanyId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.Routes.Add(newRoute);
                TempData["SuccessMessage"] = "Thêm mới lộ trình thành công.";
            }
            else
            {
                var routeToUpdate = await _context.Routes.FindAsync(RouteInput.RouteId);
                if (routeToUpdate == null)
                {
                    return NotFound();
                }
                if (routeToUpdate.Departure != RouteInput.Departure || routeToUpdate.Destination != RouteInput.Destination)
                {
                    bool routeExists = await _context.Routes.AnyAsync(r => r.RouteId != RouteInput.RouteId && r.Departure == RouteInput.Departure && r.Destination == RouteInput.Destination);
                    if (routeExists)
                    {
                        ModelState.AddModelError(string.Empty, $"Lộ trình từ '{RouteInput.Departure}' đến '{RouteInput.Destination}' đã tồn tại.");
                        ViewData["Title"] = "Chỉnh sửa Lộ trình";
                        return Page();
                    }
                }
                routeToUpdate.Departure = RouteInput.Departure;
                routeToUpdate.Destination = RouteInput.Destination;
                routeToUpdate.OriginAddress = RouteInput.OriginAddress;
                routeToUpdate.DestinationAddress = RouteInput.DestinationAddress;
                routeToUpdate.OriginCoordinates = RouteInput.OriginCoordinates;
                routeToUpdate.DestinationCoordinates = RouteInput.DestinationCoordinates;
                routeToUpdate.Distance = RouteInput.Distance;
                routeToUpdate.EstimatedDuration = estimatedDurationTimeSpan;
                routeToUpdate.Status = RouteInput.Status;
                routeToUpdate.ProposedByCompanyId = RouteInput.ProposedByCompanyId;
                routeToUpdate.UpdatedAt = DateTime.UtcNow;
                _context.Attach(routeToUpdate).State = EntityState.Modified;
                TempData["SuccessMessage"] = "Đã cập nhật thông tin lộ trình thành công.";
            }
            await _context.SaveChangesAsync();
            return RedirectToPage("./Index");
        }
    }
    public class RouteInputModel {
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
        [Required]
        [Display(Name = "Trạng thái")]
        public RouteStatus Status { get; set; } = RouteStatus.PendingApproval;
        [Display(Name = "Đề xuất bởi nhà xe")]
        public int? ProposedByCompanyId{ get; set; }
    }
}