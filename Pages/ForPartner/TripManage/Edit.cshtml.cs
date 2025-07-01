using BusTicketSystem.Data;
using BusTicketSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace BusTicketSystem.Pages.ForPartner.TripManage
{
    [Authorize(Roles = "Partner")]
    public class EditModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public EditModel(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }
#pragma warning disable CS8603 // Possible null reference return.
        public string MapboxAccessToken => _configuration["Mapbox:AccessToken"] ?? string.Empty;
#pragma warning restore CS8603 // Possible null reference return.

        [BindProperty]
        public TripInputModel TripInput { get; set; } = new TripInputModel();

        public List<SelectListItem> Routes { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> Buses { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> Drivers { get; set; } = new List<SelectListItem>();
        public string PartnerCompanyName { get; set; } = string.Empty;

        private int GetCurrentPartnerBusCompanyId()
        {
            var busCompanyIdClaim = User.FindFirstValue("CompanyId");
            if (int.TryParse(busCompanyIdClaim, out int busCompanyId))
            {
                return busCompanyId;
            }
            throw new Exception("BusCompanyId not found for the current partner.");
        }

        private async Task LoadPartnerCompanyName(int companyId)
        {
            var company = await _context.BusCompanies.FindAsync(companyId);
            PartnerCompanyName = company?.CompanyName ?? "Không xác định";
        }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            int partnerBusCompanyId = GetCurrentPartnerBusCompanyId();
            await LoadPartnerCompanyName(partnerBusCompanyId);

            ViewData["Stops"] = await _context.Stops
                .Where(s => s.CompanyId == partnerBusCompanyId || (s.CompanyId == null && s.Status == StopStatus.Approved))
                .Select(s => new SelectListItem { Value = s.StopId.ToString(), Text = s.StopName })
                .ToListAsync();

            // Fetch routes and sort by Departure and Destination in the database
            var routeData = await _context.Routes
                .Where(r => r.Status == RouteStatus.Approved)
                .OrderBy(r => r.Departure)
                .ThenBy(r => r.Destination)
                .Select(r => new { r.RouteId, r.Departure, r.Destination })
                .ToListAsync();

            // Construct SelectListItem client-side
            Routes = routeData
                .Select(r => new SelectListItem
                {
                    Value = r.RouteId.ToString(),
                    Text = $"{r.Departure} \u2192 {r.Destination} (ID: {r.RouteId})"
                })
                .ToList();

            Buses = await _context.Buses
                .Where(b => b.CompanyId == partnerBusCompanyId && b.Status == BusStatus.Active)
                .Select(b => new SelectListItem { Value = b.BusId.ToString(), Text = $"{b.LicensePlate} ({b.BusType} - {b.Capacity} ghế)" })
                .ToListAsync();

            Drivers = await _context.Drivers
                .Where(d => d.CompanyId == partnerBusCompanyId && d.Status == DriverStatus.Active)
                .Select(d => new SelectListItem { Value = d.DriverId.ToString(), Text = $"{d.Fullname} (ID: {d.DriverId})" })
                .ToListAsync();

            if (id == null)
            {
                ViewData["Title"] = "Đề xuất Chuyến đi mới";
                TripInput = new TripInputModel
                {
                    CompanyId = partnerBusCompanyId,
                    Status = TripStatus.PendingApproval,
                    DepartureTime = DateTime.Now.Date.AddDays(1).AddHours(8),
                    TripStops = new List<TripStopInputModel>()
                };
                return Page();
            }

            ViewData["Title"] = "Chỉnh sửa Chuyến đi";
            var trip = await _context.Trips
                .Include(t => t.TripStops)
                .FirstOrDefaultAsync(t => t.TripId == id && t.CompanyId == partnerBusCompanyId);

            if (trip == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy chuyến đi hoặc bạn không có quyền chỉnh sửa.";
                return RedirectToPage("./Index");
            }

            if (trip.Status != TripStatus.PendingApproval && trip.Status != TripStatus.Rejected)
            {
                TempData["ErrorMessage"] = "Bạn chỉ có thể chỉnh sửa chuyến đi đang chờ duyệt hoặc bị từ chối.";
                return RedirectToPage("./Index");
            }

            TripInput = new TripInputModel
            {
                TripId = trip.TripId,
                RouteId = trip.RouteId,
                BusId = trip.BusId,
                DriverId = trip.DriverId,
                CompanyId = trip.CompanyId,
                DepartureTime = trip.DepartureTime,
                Price = trip.Price,
                InitialAvailableSeats = trip.AvailableSeats,
                Status = trip.Status,
                CancellationReason = trip.CancellationReason,
                TripStops = trip.TripStops.Select(ts => new TripStopInputModel
                {
                    StopId = ts.StopId,
                    StopOrder = ts.StopOrder,
                    EstimatedArrival = ts.EstimatedArrival,
                    EstimatedDeparture = ts.EstimatedDeparture,
                    Note = ts.Note,
                    Latitude = ts.Latitude,
                    Longitude = ts.Longitude
                }).ToList()
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            int partnerBusCompanyId = GetCurrentPartnerBusCompanyId();
            await LoadPartnerCompanyName(partnerBusCompanyId);
            TripInput.CompanyId = partnerBusCompanyId;

            if (TripInput.DepartureTime <= DateTime.UtcNow.AddHours(1))
            {
                ModelState.AddModelError("TripInput.DepartureTime", "Thời gian khởi hành phải sau ít nhất 1 giờ nữa.");
            }

            var bus = await _context.Buses.FirstOrDefaultAsync(b => b.BusId == TripInput.BusId && b.CompanyId == partnerBusCompanyId);
            if (bus == null)
            {
                ModelState.AddModelError("TripInput.BusId", "Xe không hợp lệ hoặc không thuộc nhà xe của bạn.");
            }
            else
            {
                TripInput.InitialAvailableSeats = bus.Capacity;
            }

            var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.DriverId == TripInput.DriverId && d.CompanyId == partnerBusCompanyId);
            if (driver == null)
            {
                ModelState.AddModelError("TripInput.DriverId", "Tài xế không hợp lệ hoặc không thuộc nhà xe của bạn.");
            }

            bool busConflict = await _context.Trips.AnyAsync(t => t.BusId == TripInput.BusId && t.TripId != TripInput.TripId &&
                                                                t.CompanyId == partnerBusCompanyId &&
                                                                (t.Status == TripStatus.Scheduled || t.Status == TripStatus.PendingApproval) &&
                                                                t.DepartureTime < TripInput.DepartureTime.AddHours(4) &&
                                                                t.DepartureTime.AddHours(4) > TripInput.DepartureTime);
            if (busConflict)
            {
                ModelState.AddModelError("TripInput.BusId", "Xe này đã có lịch trình bị trùng thời gian.");
            }

            bool driverConflict = await _context.Trips.AnyAsync(t => t.DriverId == TripInput.DriverId && t.TripId != TripInput.TripId &&
                                                                t.CompanyId == partnerBusCompanyId &&
                                                                (t.Status == TripStatus.Scheduled || t.Status == TripStatus.PendingApproval) &&
                                                                t.DepartureTime < TripInput.DepartureTime.AddHours(8) &&
                                                                t.DepartureTime.AddHours(8) > TripInput.DepartureTime);
            if (driverConflict)
            {
                ModelState.AddModelError("TripInput.DriverId", "Tài xế này đã có lịch trình bị trùng thời gian.");
            }

            var route = await _context.Routes.FirstOrDefaultAsync(r => r.RouteId == TripInput.RouteId && r.Status == RouteStatus.Approved);
            if (route == null)
            {
                ModelState.AddModelError("TripInput.RouteId", "Lộ trình không hợp lệ hoặc chưa được duyệt.");
            }

            if (TripInput.TripStops != null)
            {
                var stopIds = TripInput.TripStops.Select(ts => ts.StopId).ToList();
                if (stopIds.Distinct().Count() != stopIds.Count)
                {
                    ModelState.AddModelError("TripInput.TripStops", "Không được chọn trùng trạm dừng.");
                }

                var stopOrders = TripInput.TripStops.Select(ts => ts.StopOrder).ToList();
                if (stopOrders.Distinct().Count() != stopOrders.Count)
                {
                    ModelState.AddModelError("TripInput.TripStops", "Thứ tự dừng không được trùng lặp.");
                }

                foreach (var stopInput in TripInput.TripStops)
                {
                    var stop = await _context.Stops.FirstOrDefaultAsync(s => s.StopId == stopInput.StopId &&
                        (s.CompanyId == partnerBusCompanyId || (s.CompanyId == null && s.Status == StopStatus.Approved)));
                    if (stop == null)
                    {
                        ModelState.AddModelError($"TripInput.TripStops[{TripInput.TripStops.IndexOf(stopInput)}].StopId",
                            "Trạm dừng không hợp lệ hoặc không thuộc nhà xe của bạn.");
                    }
                }
            }

            if (!ModelState.IsValid)
            {
                // Fetch routes and sort by Departure and Destination in the database
                var routeData = await _context.Routes
                    .Where(r => r.Status == RouteStatus.Approved)
                    .OrderBy(r => r.Departure)
                    .ThenBy(r => r.Destination)
                    .Select(r => new { r.RouteId, r.Departure, r.Destination })
                    .ToListAsync();

                // Construct SelectListItem client-side
                Routes = routeData
                    .Select(r => new SelectListItem
                    {
                        Value = r.RouteId.ToString(),
                        Text = $"{r.Departure} \u2192 {r.Destination} (ID: {r.RouteId})"
                    })
                    .ToList();

                Buses = await _context.Buses
                    .Where(b => b.CompanyId == partnerBusCompanyId && b.Status == BusStatus.Active)
                    .Select(b => new SelectListItem { Value = b.BusId.ToString(), Text = $"{b.LicensePlate} ({b.BusType} - {b.Capacity} ghế)" })
                    .ToListAsync();

                Drivers = await _context.Drivers
                    .Where(d => d.CompanyId == partnerBusCompanyId && d.Status == DriverStatus.Active)
                    .Select(d => new SelectListItem { Value = d.DriverId.ToString(), Text = $"{d.Fullname} (ID: {d.DriverId})" })
                    .ToListAsync();

                ViewData["Stops"] = await _context.Stops
                    .Where(s => s.CompanyId == partnerBusCompanyId || (s.CompanyId == null && s.Status == StopStatus.Approved))
                    .Select(s => new SelectListItem { Value = s.StopId.ToString(), Text = s.StopName })
                    .ToListAsync();

                ViewData["Title"] = TripInput.TripId == 0 ? "Đề xuất Chuyến đi mới" : "Chỉnh sửa Chuyến đi";
                return Page();
            }

            string successMessage;
            string notificationMessageToAdmin;

            if (TripInput.TripId == 0)
            {
                var newTrip = new Trip
                {
                    RouteId = TripInput.RouteId,
                    BusId = TripInput.BusId,
                    DriverId = TripInput.DriverId,
                    CompanyId = partnerBusCompanyId,
                    DepartureTime = TripInput.DepartureTime,
                    Price = TripInput.Price,
                    AvailableSeats = TripInput.InitialAvailableSeats,
                    Status = TripStatus.PendingApproval,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Trips.Add(newTrip);
                await _context.SaveChangesAsync();

                if (TripInput.TripStops != null)
                {
                    foreach (var stopInput in TripInput.TripStops)
                    {
                        var stop = await _context.Stops.FindAsync(stopInput.StopId);
                        if (stop != null)
                        {
                            _context.TripStops.Add(new TripStop
                            {
                                TripId = newTrip.TripId,
                                StopId = stopInput.StopId,
                                StopOrder = stopInput.StopOrder,
                                StationName = stop.StopName,
                                Latitude = stop.Latitude,
                                Longitude = stop.Longitude,
                                EstimatedArrival = stopInput.EstimatedArrival,
                                EstimatedDeparture = stopInput.EstimatedDeparture,
                                Note = stopInput.Note
                            });
                        }
                    }
                }

                successMessage = "Đã gửi đề xuất chuyến đi mới thành công. Chờ quản trị viên duyệt.";
                var routeInfo = await _context.Routes.FindAsync(newTrip.RouteId);
                notificationMessageToAdmin = $"Nhà xe '{PartnerCompanyName}' đã đề xuất chuyến đi mới: {routeInfo?.Departure} - {routeInfo?.Destination}, khởi hành {newTrip.DepartureTime:g}.";
            }
            else
            {
                var tripToUpdate = await _context.Trips
                    .Include(t => t.TripStops)
                    .FirstOrDefaultAsync(t => t.TripId == TripInput.TripId && t.CompanyId == partnerBusCompanyId);

                if (tripToUpdate == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy chuyến đi hoặc bạn không có quyền chỉnh sửa.";
                    return RedirectToPage("./Index");
                }

                if (tripToUpdate.Status != TripStatus.PendingApproval && tripToUpdate.Status != TripStatus.Rejected)
                {
                    TempData["ErrorMessage"] = "Không thể chỉnh sửa chuyến đi này.";
                    return RedirectToPage("./Index");
                }

                tripToUpdate.RouteId = TripInput.RouteId;
                tripToUpdate.BusId = TripInput.BusId;
                tripToUpdate.DriverId = TripInput.DriverId;
                tripToUpdate.DepartureTime = TripInput.DepartureTime;
                tripToUpdate.Price = TripInput.Price;
                tripToUpdate.AvailableSeats = TripInput.InitialAvailableSeats;
                tripToUpdate.Status = TripStatus.PendingApproval;
                tripToUpdate.CancellationReason = null;
                tripToUpdate.UpdatedAt = DateTime.UtcNow;

                _context.TripStops.RemoveRange(tripToUpdate.TripStops);
                if (TripInput.TripStops != null)
                {
                    foreach (var stopInput in TripInput.TripStops)
                    {
                        var stop = await _context.Stops.FindAsync(stopInput.StopId);
                        if (stop != null)
                        {
                            _context.TripStops.Add(new TripStop
                            {
                                TripId = tripToUpdate.TripId,
                                StopId = stopInput.StopId,
                                StopOrder = stopInput.StopOrder,
                                StationName = stop.StopName,
                                Latitude = stop.Latitude,
                                Longitude = stop.Longitude,
                                EstimatedArrival = stopInput.EstimatedArrival,
                                EstimatedDeparture = stopInput.EstimatedDeparture,
                                Note = stopInput.Note
                            });
                        }
                    }
                }

                _context.Attach(tripToUpdate).State = EntityState.Modified;
                successMessage = "Đã cập nhật và gửi lại đề xuất chuyến đi. Chờ quản trị viên duyệt.";
                var routeInfo = await _context.Routes.FindAsync(tripToUpdate.RouteId);
                notificationMessageToAdmin = $"Nhà xe '{PartnerCompanyName}' đã cập nhật đề xuất chuyến đi: {routeInfo?.Departure} - {routeInfo?.Destination}, khởi hành {tripToUpdate.DepartureTime:g}.";
            }

            await _context.SaveChangesAsync();

            var adminNotification = new BusTicketSystem.Models.Notification
            {
                Message = notificationMessageToAdmin,
                Category = NotificationCategory.Trip,
                TargetUrl = $"/ForAdmin/TripManage/Edit/{(TripInput.TripId == 0 ? _context.Trips.Local.FirstOrDefault()?.TripId : TripInput.TripId)}",
                IconCssClass = "bi bi-journal-arrow-up",
                RecipientUserId = null
            };
            _context.Notifications.Add(adminNotification);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = successMessage;
            return RedirectToPage("./Index");
        }

        public async Task<JsonResult> OnGetBusDetailsAsync(int busId)
        {
            var bus = await _context.Buses
                .Where(b => b.BusId == busId && b.Status == BusStatus.Active && b.CompanyId == GetCurrentPartnerBusCompanyId())
                .Select(b => new { b.Capacity })
                .FirstOrDefaultAsync();
            if (bus == null) return new JsonResult(new { error = "Không tìm thấy xe." }) { StatusCode = 404 };
            return new JsonResult(bus);
        }

        public async Task<JsonResult> OnGetRouteDetailsAsync(int routeId)
        {
            var route = await _context.Routes
                .Where(r => r.RouteId == routeId && r.Status == RouteStatus.Approved)
                .Select(r => new
                {
                    originCoordinates = r.OriginCoordinates,
                    destinationCoordinates = r.DestinationCoordinates,
                    originAddress = r.OriginAddress ?? r.Departure,
                    destinationAddress = r.DestinationAddress ?? r.Destination,
                    distance = r.Distance,
                    estimatedDuration = r.EstimatedDuration.HasValue ? r.EstimatedDuration.Value.ToString(@"hh\h\ mm\m") : null
                })
                .FirstOrDefaultAsync();
            if (route == null) return new JsonResult(new { error = "Lộ trình không tìm thấy hoặc chưa được duyệt." }) { StatusCode = 404 };
            return new JsonResult(route);
        }
    }

    public class TripInputModel
    {
        public int TripId { get; set; }

        [Required(ErrorMessage = "Lộ trình không được để trống.")]
        public int RouteId { get; set; }

        [Required(ErrorMessage = "Xe không được để trống.")]
        public int BusId { get; set; }

        [Required(ErrorMessage = "Tài xế không được để trống.")]
        public int DriverId { get; set; }

        public int? CompanyId { get; set; }

        [Required(ErrorMessage = "Thời gian khởi hành không được để trống.")]
        [Display(Name = "Thời gian khởi hành")]
        public DateTime DepartureTime { get; set; }

        [Required(ErrorMessage = "Giá vé không được để trống.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Giá vé phải lớn hơn 0.")]
        [Display(Name = "Giá vé")]
        public decimal Price { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Số ghế trống không hợp lệ.")]
        [Display(Name = "Số ghế trống")]
        public int? InitialAvailableSeats { get; set; }

        [Display(Name = "Trạng thái")]
        public TripStatus? Status { get; set; }

        [StringLength(500)]
        [Display(Name = "Lý do từ chối")]
        public string? CancellationReason { get; set; }

        public List<TripStopInputModel> TripStops { get; set; } = new List<TripStopInputModel>();
    }

    public class TripStopInputModel
    {
        public int StopId { get; set; }

        [Required(ErrorMessage = "Thứ tự dừng không được để trống.")]
        [Range(1, int.MaxValue, ErrorMessage = "Thứ tự dừng phải lớn hơn 0.")]
        public int StopOrder { get; set; }

        [Display(Name = "Thời gian đến dự kiến")]
        public TimeSpan? EstimatedArrival { get; set; }

        [Display(Name = "Thời gian đi dự kiến")]
        public TimeSpan? EstimatedDeparture { get; set; }

        [StringLength(500)]
        [Display(Name = "Ghi chú")]
        public string? Note { get; set; }

        [Display(Name = "Vĩ độ")]
        public double? Latitude { get; set; }

        [Display(Name = "Kinh độ")]
        public double? Longitude { get; set; }
    }
}