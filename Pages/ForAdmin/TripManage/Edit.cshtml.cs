using BusTicketSystem.Data;
using BusTicketSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using System.ComponentModel.DataAnnotations;

namespace BusTicketSystem.Pages.ForAdmin.TripManage
{
    [Authorize(Roles = "Admin")]
    public class EditModel : PageModel
    {
        private readonly IConfiguration _configuration;
        private readonly AppDbContext _context;

        public EditModel(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }
        public string MapboxAccessToken => _configuration["Mapbox:AccessToken"] ?? string.Empty;
        [BindProperty]
        public TripInputModel TripInput { get; set; } = new TripInputModel();

        public SelectList Routes { get; set; } = new SelectList(Enumerable.Empty<SelectListItem>());
        public SelectList Buses { get; set; } = new SelectList(Enumerable.Empty<SelectListItem>());
        public SelectList Drivers { get; set; } = new SelectList(Enumerable.Empty<SelectListItem>());
        public SelectList Companies { get; set; } = new SelectList(Enumerable.Empty<SelectListItem>());

        private async Task LoadSelectListsAsync(int? selectedRouteId = null, int? selectedBusId = null, int? selectedDriverId = null, int? selectedCompanyId = null)
        {
            var activeRoutes = await _context.Routes
                .Where(r => r.Status == RouteStatus.Approved)
                .OrderBy(r => r.Departure)
                .ThenBy(r => r.Destination)
                .Select(r => new { r.RouteId, Name = $"{r.Departure} \u2192 {r.Destination} (ID: {r.RouteId})" })
                .AsNoTracking()
                .ToListAsync();
            Routes = new SelectList(activeRoutes ?? Enumerable.Empty<object>(), "RouteId", "Name", selectedRouteId);

            var busQuery = _context.Buses.Where(b => b.Status == BusStatus.Active);
            if (selectedCompanyId.HasValue)
            {
                busQuery = busQuery.Where(b => b.CompanyId == selectedCompanyId.Value);
            }
            var activeBuses = await busQuery
                .OrderBy(b => b.LicensePlate)
                .Select(b => new { b.BusId, Name = $"{b.LicensePlate} ({b.BusType} - {b.Capacity} ghế)" })
                .AsNoTracking()
                .ToListAsync();
            Buses = new SelectList(activeBuses ?? Enumerable.Empty<object>(), "BusId", "Name", selectedBusId);

            var driverQuery = _context.Drivers.Where(d => d.Status == DriverStatus.Active);
            if (selectedCompanyId.HasValue)
            {
                driverQuery = driverQuery.Where(d => d.CompanyId == selectedCompanyId.Value);
            }
            var activeDrivers = await driverQuery
                .OrderBy(d => d.Fullname)
                .Select(d => new { d.DriverId, Name = $"{d.Fullname} (ID: {d.DriverId})" })
                .AsNoTracking()
                .ToListAsync();
            Drivers = new SelectList(activeDrivers, "DriverId", "Name", selectedDriverId);

            var activeCompanies = await _context.BusCompanies
                .Where(c => c.Status == BusCompanyStatus.Active)
                .OrderBy(c => c.CompanyName)
                .Select(c => new { c.CompanyId, c.CompanyName })
                .AsNoTracking()
                .ToListAsync();
            var companyListItems = activeCompanies?.Select(c => new SelectListItem
            {
                Value = c.CompanyId.ToString(),
                Text = c.CompanyName
            }).ToList() ?? new List<SelectListItem>();
            Companies = new SelectList(companyListItems, "Value", "Text", selectedCompanyId);
        }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                ViewData["Title"] = "Thêm mới Chuyến đi";
                TripInput = new TripInputModel { DepartureTime = DateTime.Now.Date.AddDays(1).AddHours(8) };
                await LoadSelectListsAsync();
            }
            else
            {
                ViewData["Title"] = "Chỉnh sửa Chuyến đi";
                var trip = await _context.Trips
                    .Include(t => t.TripStops.OrderBy(ts => ts.StopOrder))
                    .FirstOrDefaultAsync(t => t.TripId == id);
                if (trip == null) return NotFound();

                TripInput = new TripInputModel
                {
                    TripId = trip.TripId,
                    RouteId = trip.RouteId,
                    BusId = trip.BusId,
                    DriverId = trip.DriverId,
                    CompanyId = trip.CompanyId,
                    DepartureTime = trip.DepartureTime,
                    Price = trip.Price,
                    InitialAvailableSeats = trip.AvailableSeats ?? 0,
                    Status = trip.Status,
                    TripStops = trip.TripStops.Select(ts => new TripStopInputModel
                    {
                        // StopId is not present in TripStopInputModel, so we do not assign it here
                        StopOrder = ts.StopOrder,
                        EstimatedArrival = ts.EstimatedArrival,
                        EstimatedDeparture = ts.EstimatedDeparture,
                        Note = ts.Note
                    }).ToList()
                };
                await LoadSelectListsAsync(trip.RouteId, trip.BusId, trip.DriverId, trip.CompanyId);
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Chỉ kiểm tra ràng buộc thời gian khởi hành nếu tạo mới, hoặc sửa nhưng trạng thái KHÔNG phải Completed/Cancelled
            bool isCreating = TripInput.TripId == 0;
            bool isCompleting = !isCreating && TripInput.Status == TripStatus.Completed;
            if (!isCompleting && TripInput.DepartureTime <= DateTime.UtcNow.AddHours(1))
            {
                ModelState.AddModelError("TripInput.DepartureTime", "Thời gian khởi hành phải sau thời điểm hiện tại ít nhất 1 giờ.");
            }

            if (TripInput.CompanyId.HasValue)
            {
                var bus = await _context.Buses.FindAsync(TripInput.BusId);
                if (bus == null || bus.CompanyId != TripInput.CompanyId)
                {
                    ModelState.AddModelError("TripInput.BusId", "Xe được chọn không thuộc nhà xe đã chọn.");
                }

                var driver = await _context.Drivers.FindAsync(TripInput.DriverId);
                if (driver == null || driver.CompanyId != TripInput.CompanyId)
                {
                    ModelState.AddModelError("TripInput.DriverId", "Tài xế được chọn không thuộc nhà xe đã chọn.");
                }
            }

            bool busConflict = await _context.Trips.AnyAsync(t => t.BusId == TripInput.BusId && t.TripId != TripInput.TripId &&
                                                                t.Status == TripStatus.Scheduled &&
                                                                t.DepartureTime < TripInput.DepartureTime.AddHours(4) &&
                                                                t.DepartureTime.AddHours(4) > TripInput.DepartureTime);
            if (busConflict) ModelState.AddModelError("TripInput.BusId", "Xe này đã có lịch trình bị trùng thời gian.");

            if (TripInput.TripStops != null && TripInput.TripStops.Any())
            {
                var duplicateOrders = TripInput.TripStops
                    .GroupBy(ts => ts.StopOrder)
                    .Where(g => g.Count() > 1)
                    .ToList();
                if (duplicateOrders.Any())
                    ModelState.AddModelError("TripInput.TripStops", "Thứ tự dừng bị trùng lặp.");
            }

            bool driverConflict = await _context.Trips.AnyAsync(t => t.DriverId == TripInput.DriverId && t.TripId != TripInput.TripId &&
                                                                t.Status == TripStatus.Scheduled &&
                                                                t.DepartureTime < TripInput.DepartureTime.AddHours(8) &&
                                                                t.DepartureTime.AddHours(8) > TripInput.DepartureTime);
            if (driverConflict) ModelState.AddModelError("TripInput.DriverId", "Tài xế này đã có lịch trình bị trùng thời gian.");

            if (!ModelState.IsValid)
            {
                await LoadSelectListsAsync(TripInput.RouteId, TripInput.BusId, TripInput.DriverId, TripInput.CompanyId);
                ViewData["Title"] = TripInput.TripId == 0 ? "Thêm mới Chuyến đi" : "Chỉnh sửa Chuyến đi";
                TripInput.TripStops ??= new List<TripStopInputModel>();
                return Page();
            }

            string successMessage;
            string notificationMessage;

            if (TripInput.TripId == 0)
            {
                var bus = await _context.Buses.FindAsync(TripInput.BusId);
                var newTrip = new Trip
                {
                    RouteId = TripInput.RouteId!.Value,
                    BusId = TripInput.BusId!.Value,
                    DriverId = TripInput.DriverId!.Value,
                    CompanyId = TripInput.CompanyId,
                    DepartureTime = TripInput.DepartureTime,
                    Price = TripInput.Price,
                    AvailableSeats = bus?.Capacity ?? TripInput.InitialAvailableSeats,
                    Status = TripStatus.Scheduled,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.Trips.Add(newTrip);
                await _context.SaveChangesAsync();
                if (TripInput.TripStops != null)
                {
                    foreach (var stopInput in TripInput.TripStops)
                    {
                        // Tạo mới Stop từ các trường nhập trực tiếp
                        var newStop = new Stop
                        {
                            StopName = stopInput.StationName,
                            Latitude = stopInput.Latitude ?? 0,
                            Longitude = stopInput.Longitude ?? 0,
                            Address = null
                        };
                        _context.Stops.Add(newStop);
                        await _context.SaveChangesAsync();
                        // Lưu TripStop với StopId vừa tạo
                        var tripStop = new TripStop
                        {
                            TripId = newTrip.TripId,
                            StopId = newStop.StopId,
                            StopOrder = stopInput.StopOrder,
                            EstimatedArrival = stopInput.EstimatedArrival,
                            EstimatedDeparture = stopInput.EstimatedDeparture,
                            Note = stopInput.Note
                        };
                        _context.TripStops.Add(tripStop);
                    }
                }
                successMessage = "Đã thêm mới chuyến đi thành công.";
                var route = await _context.Routes.FindAsync(newTrip.RouteId);
                notificationMessage = $"Chuyến đi mới ({route?.Departure} - {route?.Destination}) đã được tạo, khởi hành lúc {newTrip.DepartureTime:dd/MM/yyyy HH:mm}.";
            }
            else
            {
                var tripToUpdate = await _context.Trips.FindAsync(TripInput.TripId);
                if (tripToUpdate == null) return NotFound();
                if (tripToUpdate.Status == TripStatus.Completed || tripToUpdate.Status == TripStatus.Cancelled)
                {
                    TempData["ErrorMessage"] = "Không thể chỉnh sửa chuyến đi đã hoàn thành hoặc đã hủy.";
                    await LoadSelectListsAsync(TripInput.RouteId, TripInput.BusId, TripInput.DriverId, TripInput.CompanyId);
                    ViewData["Title"] = "Chỉnh sửa Chuyến đi";
                    TripInput.TripStops ??= new List<TripStopInputModel>();
                    return Page();
                }
                var bus = await _context.Buses.FindAsync(TripInput.BusId);
                tripToUpdate.RouteId = TripInput.RouteId!.Value;
                tripToUpdate.BusId = TripInput.BusId!.Value;
                tripToUpdate.DriverId = TripInput.DriverId!.Value;
                tripToUpdate.CompanyId = TripInput.CompanyId;
                tripToUpdate.DepartureTime = TripInput.DepartureTime;
                tripToUpdate.Price = TripInput.Price;
                int ticketsSold = await _context.Tickets.CountAsync(t => t.TripId == tripToUpdate.TripId && t.Status == TicketStatus.Booked);
                tripToUpdate.AvailableSeats = (bus?.Capacity ?? tripToUpdate.AvailableSeats) - ticketsSold;
                tripToUpdate.Status = TripInput.Status;
                tripToUpdate.CancellationReason = TripInput.CancellationReason;
                tripToUpdate.UpdatedAt = DateTime.UtcNow;

                _context.Attach(tripToUpdate).State = EntityState.Modified;

                // Xóa hết các TripStop cũ
                var existingStops = await _context.TripStops.Where(ts => ts.TripId == tripToUpdate.TripId).ToListAsync();
                foreach (var existingStop in existingStops)
                {
                    _context.TripStops.Remove(existingStop);
                }

                // Thêm lại các TripStop mới
                var updatedStops = TripInput.TripStops ?? new List<TripStopInputModel>();
                foreach (var stopInput in updatedStops)
                {
                    // Tạo mới Stop từ các trường nhập trực tiếp
                    var newStop = new Stop
                    {
                        StopName = stopInput.StationName,
                        Latitude = stopInput.Latitude ?? 0,
                        Longitude = stopInput.Longitude ?? 0,
                        Address = null
                    };
                    _context.Stops.Add(newStop);
                    await _context.SaveChangesAsync();
                    // Lưu TripStop với StopId vừa tạo
                    var tripStop = new TripStop
                    {
                        TripId = tripToUpdate.TripId,
                        StopId = newStop.StopId,
                        StopOrder = stopInput.StopOrder,
                        EstimatedArrival = stopInput.EstimatedArrival,
                        EstimatedDeparture = stopInput.EstimatedDeparture,
                        Note = stopInput.Note
                    };
                    _context.TripStops.Add(tripStop);
                }
                successMessage = "Đã cập nhật thông tin chuyến đi thành công.";
                var route = await _context.Routes.FindAsync(tripToUpdate.RouteId);
                notificationMessage = $"Thông tin chuyến đi ({route?.Departure} - {route?.Destination}, khởi hành {tripToUpdate.DepartureTime:dd/MM/yyyy HH:mm}) đã được cập nhật.";
            }

            await _context.SaveChangesAsync();

            var notification = new Notification
            {
                Message = notificationMessage,
                Category = NotificationCategory.Trip,
                TargetUrl = Url.Page("./Index"),
                IconCssClass = "bi bi-signpost-split-fill",
                RecipientUserId = null
            };
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = successMessage;
            return RedirectToPage("./Index");
        }

        public async Task<JsonResult> OnGetRouteDetailsAsync(int routeId)
        {
            var route = await _context.Routes.FindAsync(routeId);
            if (route == null)
            {
                return new JsonResult(NotFound("Không tìm thấy lộ trình.")) { StatusCode = 404 };
            }
            return new JsonResult(new
            {
                originCoordinates = route.OriginCoordinates,
                destinationCoordinates = route.DestinationCoordinates,
                originAddress = route.OriginAddress ?? route.Departure,
                destinationAddress = route.DestinationAddress ?? route.Destination,
                distance = route.Distance,
                estimatedDuration = route.EstimatedDuration?.ToString(@"hh\h\ mm\m")
            });
        }

        public async Task<JsonResult> OnGetDriversByCompanyAsync(int? companyId)
        {
            var query = _context.Drivers.Where(d => d.Status == DriverStatus.Active);
            if (companyId.HasValue)
            {
                query = query.Where(d => d.CompanyId == companyId.Value);
            }
            var drivers = await query
                .OrderBy(d => d.Fullname)
                .Select(d => new { id = d.DriverId, name = $"{d.Fullname} (ID: {d.DriverId})" })
                .ToListAsync();
            return new JsonResult(drivers);
        }
        public async Task<JsonResult> OnGetBusesByCompanyAsync(int? companyId)
        {
            var query = _context.Buses.Where(b => b.Status == BusStatus.Active);
            if (companyId.HasValue)
            {
                query = query.Where(b => b.CompanyId == companyId.Value);
            }
            var buses = await query
                .OrderBy(b => b.LicensePlate)
                .Select(b => new { id = b.BusId, name = $"{b.LicensePlate} ({b.BusType} - {b.Capacity} ghế)" })
                .ToListAsync();
            return new JsonResult(buses);
        }
        public async Task<JsonResult> OnGetRoutesByCompanyAsync(int? companyId)
        {
            var query = _context.Routes.Where(r => r.Status == RouteStatus.Approved);
            if (companyId.HasValue)
            {
                query = query.Where(r => r.ProposedByCompanyId == companyId.Value);
            }
            var routes = await query
                .OrderBy(r => r.Departure)
                .ThenBy(r => r.Destination)
                .Select(r => new { id = r.RouteId, name = $"{r.Departure} \u2192 {r.Destination} (ID: {r.RouteId})" })
                .ToListAsync();
            return new JsonResult(routes);
        }
        public async Task<JsonResult> OnGetBusDetailsAsync(int busId)
        {
            var bus = await _context.Buses
                .Where(b => b.BusId == busId && b.Status == BusStatus.Active)
                .Select(b => new { b.Capacity })
                .FirstOrDefaultAsync();
            if (bus == null) return new JsonResult(NotFound("Không tìm thấy xe.")) { StatusCode = 404 };
            return new JsonResult(bus);
        }
    }

    public class TripInputModel
    {
        public int TripId { get; set; }

        [Required(ErrorMessage = "Bắt buộc phải chọn lộ trình.")]
        [Display(Name = "Lộ trình")]
        public int? RouteId { get; set; }

        [Required(ErrorMessage = "Bắt buộc phải chọn xe.")]
        [Display(Name = "Xe")]
        public int? BusId { get; set; }

        [Required(ErrorMessage = "Bắt buộc phải chọn tài xế.")]
        [Display(Name = "Tài xế")]
        public int? DriverId { get; set; }

        [Display(Name = "Nhà xe (Nếu có)")]
        public int? CompanyId { get; set; }

        [Required(ErrorMessage = "Thời gian khởi hành không được để trống.")]
        [Display(Name = "Thời gian khởi hành")]
        public DateTime DepartureTime { get; set; } = DateTime.Now.AddDays(1).AddHours(7);

        [Required(ErrorMessage = "Giá vé không được để trống.")]
        [Range(10000, 10000000, ErrorMessage = "Giá vé phải từ 10.000 đến 10.000.000 VNĐ.")]
        [Display(Name = "Giá vé (VNĐ)")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Số ghế ban đầu không được để trống.")]
        [Range(1, 100, ErrorMessage = "Số ghế phải từ 1 đến 100.")]
        [Display(Name = "Số ghế ban đầu (sẽ lấy theo xe nếu có)")]
        public int InitialAvailableSeats { get; set; }

        [Display(Name = "Trạng thái")]
        public TripStatus Status { get; set; } = TripStatus.Scheduled;

        [Display(Name = "Điểm dừng chân")]
        public List<TripStopInputModel> TripStops { get; set; } = new List<TripStopInputModel>();

        [Display(Name = "Lý do từ chối/hủy")]
        public string? CancellationReason { get; set; }
    }

    public class TripStopInputModel
    {
        // Không còn dùng StopId, thay bằng nhập trực tiếp các trường bên dưới
        [Required(ErrorMessage = "Tên trạm dừng không được để trống.")]
        [Display(Name = "Tên trạm dừng")]
        public string StationName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vĩ độ không được để trống.")]
        [Display(Name = "Vĩ độ")]
        public double? Latitude { get; set; }

        [Required(ErrorMessage = "Kinh độ không được để trống.")]
        [Display(Name = "Kinh độ")]
        public double? Longitude { get; set; }

        [Required(ErrorMessage = "Thứ tự dừng không được để trống.")]
        [Range(1, int.MaxValue, ErrorMessage = "Thứ tự dừng phải lớn hơn 0.")]
        [Display(Name = "Thứ tự")]
        public int StopOrder { get; set; }

        [Display(Name = "Đến (dự kiến)")]
        [DataType(DataType.Time)]
        public TimeSpan? EstimatedArrival { get; set; }

        [Display(Name = "Đi (dự kiến)")]
        [DataType(DataType.Time)]
        public TimeSpan? EstimatedDeparture { get; set; }

        [StringLength(500)]
        [Display(Name = "Ghi chú")]
        public string? Note { get; set; }

        // Không dùng nữa, logic tạo TripStop đã chuyển vào OnPostAsync
    }
}