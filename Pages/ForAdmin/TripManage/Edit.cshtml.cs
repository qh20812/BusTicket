using BusTicketSystem.Data;
using BusTicketSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Globalization;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace BusTicketSystem.Pages.ForAdmin.TripManage
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
        public TripInputModel TripInput { get; set; } = new TripInputModel();

        public SelectList Routes { get; set; } = new SelectList(Enumerable.Empty<SelectListItem>());
        public SelectList Buses { get; set; } = new SelectList(Enumerable.Empty<SelectListItem>());
        public SelectList Drivers { get; set; } = new SelectList(Enumerable.Empty<SelectListItem>());
        public SelectList Companies { get; set; } = new SelectList(Enumerable.Empty<SelectListItem>());


        private async Task LoadSelectListsAsync(int? selectedRouteId = null, int? selectedBusId = null, int? selectedDriverId = null, int? selectedCompanyId = null, bool filterDriversByCompany = false)
        {
            var activeRoutes = await _context.Routes
                                .OrderBy(r => r.Departure)
                                .ThenBy(r => r.Destination)
                                .Select(r => new { r.RouteId, Name = $"{r.Departure} \u2192 {r.Destination} (ID: {r.RouteId})" })
                                .AsNoTracking() // Thêm AsNoTracking nếu chỉ đọc dữ liệu
                                .ToListAsync();
            Routes = new SelectList(activeRoutes ?? Enumerable.Empty<object>(), "RouteId", "Name", selectedRouteId);

            // Chỉ lấy xe đang hoạt động và chưa được gán cho chuyến nào khác trùng thời gian (logic phức tạp hơn, tạm thời lấy hết xe active)
            var activeBuses = await _context.Buses
                                .Where(b => b.Status == BusStatus.Active) // Giả sử Bus có Status
                                .OrderBy(b => b.LicensePlate)
                                .Select(b => new { b.BusId, Name = $"{b.LicensePlate} ({b.BusType} - {b.Capacity} ghế)" })
                                .AsNoTracking()
                                .ToListAsync();
            Buses = new SelectList(activeBuses ?? Enumerable.Empty<object>(), "BusId", "Name", selectedBusId);

            IQueryable<Driver> driversQuery = _context.Drivers.Where(d => d.Status == DriverStatus.Active);

            if (filterDriversByCompany && selectedCompanyId.HasValue)
            {
                driversQuery = driversQuery.Where(d => d.CompanyId == selectedCompanyId.Value);
            }
            else if (filterDriversByCompany && !selectedCompanyId.HasValue)
            {
                // Nếu filterDriversByCompany là true nhưng không có selectedCompanyId (ví dụ: người dùng xóa chọn nhà xe),
                // thì sẽ hiển thị danh sách rỗng nếu tất cả tài xế đều thuộc nhà xe nào đó.
                // Hoặc, có thể hiển thị tất cả tài xế active (hành vi hiện tại nếu filterDriversByCompany = false).
                // Hiện tại, để đơn giản, nếu không có companyId cụ thể để lọc, sẽ không áp dụng thêm điều kiện lọc CompanyId.
            }

            var activeDrivers = await driversQuery.OrderBy(d => d.Fullname)
                                     .Select(d => new { d.DriverId, Name = $"{d.Fullname} (ID: {d.DriverId})" })
                                     .AsNoTracking()
                                     .ToListAsync();
            Drivers = new SelectList(activeDrivers, "DriverId", "Name", selectedDriverId);

            var activeCompanies = await _context.BusCompanies
                                .Where(c => c.Status == BusCompanyStatus.Active) // Giả sử BusCompany có Status
                                .OrderBy(c => c.CompanyName)
                                .Select(c => new { c.CompanyId, c.CompanyName }) // Chọn các thuộc tính cần thiết
                                .AsNoTracking()
                                .ToListAsync();

            // Tạo danh sách SelectListItem cho Companies để có thể thêm mục "Không có"
            var companyListItems = activeCompanies?.Select(c => new SelectListItem
            {
                Value = c.CompanyId.ToString(),
                Text = c.CompanyName
            }).ToList() ?? new List<SelectListItem>();

            // Thêm mục "Không có" vào đầu danh sách nếu cần thiết (hoặc để trống optionLabel trong select tag)
            // Nếu bạn muốn "Không có" là một lựa chọn có giá trị null hoặc 0:
            // companyListItems.Insert(0, new SelectListItem { Value = "", Text = "Không có" });
            // Hoặc dựa vào optionLabel trong tag select HTML: <option value="">-- Chọn nhà xe (nếu có) --</option>

            Companies = new SelectList(companyListItems, "Value", "Text", selectedCompanyId);
        }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) // Thêm mới
            {
                ViewData["Title"] = "Thêm mới Chuyến đi";
                TripInput = new TripInputModel { DepartureTime = DateTime.Now.Date.AddDays(1).AddHours(8) }; // Mặc định ngày mai, 8h sáng
                await LoadSelectListsAsync(filterDriversByCompany: TripInput.CompanyId.HasValue, selectedCompanyId: TripInput.CompanyId);
            }
            else // Chỉnh sửa
            {
                ViewData["Title"] = "Chỉnh sửa Chuyến đi";
                var trip = await _context.Trips
                    .Include(t => t.TripStops.OrderBy(ts => ts.StopOrder)) // Load các điểm dừng và sắp xếp
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
                    InitialAvailableSeats = trip.AvailableSeats.Value, // Hoặc lấy từ Bus.Capacity nếu là logic khởi tạo
                    Status = trip.Status,
                    // Ánh xạ TripStop entities sang TripStopInputModel
                    TripStops = trip.TripStops.Select(ts => new TripStopInputModel
                    {
                        StopId = ts.StopId,
                        StopOrder = ts.StopOrder,
                        StationName = ts.StationName,
                        Latitude = ts.Latitude,
                        Longitude = ts.Longitude,
                        EstimatedArrival = ts.EstimatedArrival,
                        EstimatedDeparture = ts.EstimatedDeparture,
                        Note = ts.Note
                    }).ToList()
                };
                await LoadSelectListsAsync(trip.RouteId, trip.BusId, trip.DriverId, trip.CompanyId, filterDriversByCompany: trip.CompanyId.HasValue);
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (TripInput.DepartureTime <= DateTime.UtcNow.AddHours(1)) // Phải khởi hành sau ít nhất 1 giờ nữa
            {
                ModelState.AddModelError("TripInput.DepartureTime", "Thời gian khởi hành phải sau thời điểm hiện tại ít nhất 1 giờ.");
            }

            bool busConflict = await _context.Trips.AnyAsync(t => t.BusId == TripInput.BusId && t.TripId != TripInput.TripId &&
                                                                t.Status == TripStatus.Scheduled &&
                                                                t.DepartureTime < TripInput.DepartureTime.AddHours(4) && // Giả sử 1 chuyến kéo dài 4h
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
                                                                t.DepartureTime < TripInput.DepartureTime.AddHours(8) && // Giả sử tài xế cần nghỉ 8h giữa các chuyến
                                                                t.DepartureTime.AddHours(8) > TripInput.DepartureTime);
            if (driverConflict) ModelState.AddModelError("TripInput.DriverId", "Tài xế này đã có lịch trình bị trùng thời gian.");

            if (!ModelState.IsValid)
            {
                await LoadSelectListsAsync(TripInput.RouteId, TripInput.BusId, TripInput.DriverId, TripInput.CompanyId, filterDriversByCompany: TripInput.CompanyId.HasValue);
                ViewData["Title"] = TripInput.TripId == 0 ? "Thêm mới Chuyến đi" : "Chỉnh sửa Chuyến đi";

                TripInput.TripStops ??= new List<TripStopInputModel>();
                return Page();
            }

            string successMessage;
            string notificationMessage;

            if (TripInput.TripId == 0)
            {
                var bus = await _context.Buses.FindAsync(TripInput.BusId);
                // Tạo chuyến đi mới
#pragma warning disable CS8629 // Nullable value type may be null.
                var newTrip = new Trip
                {
                    RouteId = TripInput.RouteId.Value,
                    BusId = TripInput.BusId.Value,
                    DriverId = TripInput.DriverId.Value,
                    CompanyId = TripInput.CompanyId,
                    DepartureTime = TripInput.DepartureTime,
                    Price = TripInput.Price,
                    AvailableSeats = bus?.Capacity ?? TripInput.InitialAvailableSeats, // Lấy capacity từ bus nếu có
                    Status = TripStatus.Scheduled, // Mặc định
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
#pragma warning restore CS8629 // Nullable value type may be null.
                _context.Trips.Add(newTrip);
                await _context.SaveChangesAsync();
                if (TripInput.TripStops != null)
                {
                    foreach (var stopInput in TripInput.TripStops)
                    {
                        _context.TripStops.Add(stopInput.ToTripStop(newTrip.TripId));
                    }
                }
                successMessage = "Đã thêm mới chuyến đi thành công.";
                // Lấy tên lộ trình cho thông báo
                var route = await _context.Routes.FindAsync(newTrip.RouteId);
                notificationMessage = $"Chuyến đi mới ({route?.Departure} - {route?.Destination}) đã được tạo, khởi hành lúc {newTrip.DepartureTime:dd/MM/yyyy HH:mm}.";
            }
            else // Chỉnh sửa
            {
                var tripToUpdate = await _context.Trips.FindAsync(TripInput.TripId);
                if (tripToUpdate == null) return NotFound();
                if (tripToUpdate.Status == TripStatus.Completed || tripToUpdate.Status == TripStatus.Cancelled)
                {
                    TempData["ErrorMessage"] = "Không thể chỉnh sửa chuyến đi đã hoàn thành hoặc đã hủy.";
                    await LoadSelectListsAsync(TripInput.RouteId, TripInput.BusId, TripInput.DriverId, TripInput.CompanyId, filterDriversByCompany: TripInput.CompanyId.HasValue);
                    ViewData["Title"] = "Chỉnh sửa Chuyến đi"; // Đặt lại title
                    TripInput.TripStops ??= new List<TripStopInputModel>(); // Đảm bảo TripStops không null
                    return Page();
                }
                var bus = await _context.Buses.FindAsync(TripInput.BusId);

#pragma warning disable CS8629 // Nullable value type may be null.
                tripToUpdate.RouteId = TripInput.RouteId.Value;
#pragma warning restore CS8629 // Nullable value type may be null.
#pragma warning disable CS8629 // Nullable value type may be null.
                tripToUpdate.BusId = TripInput.BusId.Value;
#pragma warning restore CS8629 // Nullable value type may be null.
#pragma warning disable CS8629 // Nullable value type may be null.
                tripToUpdate.DriverId = TripInput.DriverId.Value;
#pragma warning restore CS8629 // Nullable value type may be null.
                tripToUpdate.CompanyId = TripInput.CompanyId;
                tripToUpdate.DepartureTime = TripInput.DepartureTime;
                tripToUpdate.Price = TripInput.Price;
                int ticketsSold = await _context.Tickets.CountAsync(t => t.TripId == tripToUpdate.TripId && t.Status == TicketStatus.Booked);
                tripToUpdate.AvailableSeats = (bus?.Capacity ?? tripToUpdate.AvailableSeats) - ticketsSold;

                tripToUpdate.Status = TripInput.Status;
                tripToUpdate.UpdatedAt = DateTime.UtcNow;

                _context.Attach(tripToUpdate).State = EntityState.Modified;

                // Xử lý cập nhật TripStops
                var existingStops = await _context.TripStops.Where(ts => ts.TripId == tripToUpdate.TripId).ToListAsync();
                var updatedStops = TripInput.TripStops ?? new List<TripStopInputModel>();

                // Xóa các điểm dừng cũ không còn trong danh sách mới
                foreach (var existingStop in existingStops)
                {
                    if (!updatedStops.Any(us => us.StopId == existingStop.StopId && us.StopId != 0))
                    {
                        _context.TripStops.Remove(existingStop);
                    }
                }

                // Thêm mới hoặc cập nhật các điểm dừng trong danh sách mới
                foreach (var updatedStopInput in updatedStops)
                {
                    if (updatedStopInput.StopId == 0) // Thêm mới
                    {
                        _context.TripStops.Add(updatedStopInput.ToTripStop(tripToUpdate.TripId));
                    }
                    else // Cập nhật
                    {
                        _context.Entry(updatedStopInput.ToTripStop(tripToUpdate.TripId)).State = EntityState.Modified;
                    }
                }
                successMessage = "Đã cập nhật thông tin chuyến đi thành công.";
                var route = await _context.Routes.FindAsync(tripToUpdate.RouteId);
                notificationMessage = $"Thông tin chuyến đi ({route?.Departure} - {route?.Destination}, khởi hành {tripToUpdate.DepartureTime:dd/MM/yyyy HH:mm}) đã được cập nhật.";
            }

            await _context.SaveChangesAsync();

            // Tạo thông báo
            var notification = new Notification
            {
                Message = notificationMessage,
                Category = NotificationCategory.Trip,
                TargetUrl = Url.Page("./Index"),
                IconCssClass = "bi bi-signpost-split-fill",
                RecipientUserId = null // Reverted
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
            if (!string.IsNullOrEmpty(route.OriginCoordinates) && !string.IsNullOrEmpty(route.DestinationCoordinates))
            {
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
            return new JsonResult(new
            {
                originAddress = route.OriginAddress ?? route.Departure,
                destinationAddress = route.DestinationAddress ?? route.Destination,
                distance = route.Distance,
                estimatedDuration = route.EstimatedDuration?.ToString(@"hh\h\ mm\m")
            });
        }



        public async Task<JsonResult> OnGetDriversByCompanyAsync(int? companyId)
        {
            IQueryable<Driver> query = _context.Drivers.Where(d => d.Status == DriverStatus.Active);
            if (companyId.HasValue)
            {
                query = query.Where(d => d.CompanyId == companyId.Value);
            }
            else
            {
                // Nếu không có companyId (ví dụ: người dùng chọn "-- Chọn nhà xe --"),
                // trả về tất cả tài xế đang hoạt động.
                // Hoặc bạn có thể quyết định trả về danh sách rỗng hoặc chỉ tài xế chưa thuộc nhà xe nào.
                // query = query.Where(d => d.CompanyId == null); // Ví dụ: chỉ tài xế chưa có nhà xe
            }
            var drivers = await query.OrderBy(d => d.Fullname)
                                   .Select(d => new { id = d.DriverId, name = $"{d.Fullname} (ID: {d.DriverId})" }).ToListAsync();
            return new JsonResult(drivers);
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
        public DateTime DepartureTime { get; set; } = DateTime.Now.AddDays(1).AddHours(7); // Default

        [Required(ErrorMessage = "Giá vé không được để trống.")]
        [Range(10000, 10000000, ErrorMessage = "Giá vé phải từ 10.000 đến 10.000.000 VNĐ.")]
        [Display(Name = "Giá vé (VNĐ)")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Số ghế ban đầu không được để trống.")]
        [Range(1, 70, ErrorMessage = "Số ghế phải từ 1 đến 70.")] // Điều chỉnh giới hạn trên
        [Display(Name = "Số ghế ban đầu (sẽ lấy theo xe nếu có)")]
        public int InitialAvailableSeats { get; set; }

        [Display(Name = "Trạng thái")]
        public TripStatus Status { get; set; } = TripStatus.Scheduled;

        // Các trường cho Google Maps khi tạo/sửa Route (nếu tích hợp sâu hơn)
        // public string? GMapOriginAddress { get; set; }
        // public string? GMapDestinationAddress { get; set; }

        [Display(Name = "Điểm dừng chân")]
        public List<TripStopInputModel> TripStops { get; set; } = new List<TripStopInputModel>();

        [Display(Name = "Lý do từ chối/hủy")] // Thêm thuộc tính này
        public string? CancellationReason { get; set; }
    }

    // ViewModel cho từng điểm dừng chân trong form
    public class TripStopInputModel
    {
        public int StopId { get; set; } // 0 cho điểm dừng mới

        [Required(ErrorMessage = "Thứ tự dừng không được để trống.")]
        [Range(1, int.MaxValue, ErrorMessage = "Thứ tự dừng phải lớn hơn 0.")]
        [Display(Name = "Thứ tự")]
        public int StopOrder { get; set; }

        [Required(ErrorMessage = "Tên trạm dừng không được để trống.")]
        [StringLength(255)]
        [Display(Name = "Tên trạm")]
        public string StationName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vĩ độ không được để trống.")]
        [Range(-90.0, 90.0, ErrorMessage = "Vĩ độ không hợp lệ.")]
        [Display(Name = "Vĩ độ")]
        public double Latitude { get; set; }

        [Required(ErrorMessage = "Kinh độ không được để trống.")]
        [Range(-180.0, 180.0, ErrorMessage = "Kinh độ không hợp lệ.")]
        [Display(Name = "Kinh độ")]
        public double Longitude { get; set; }

        [Display(Name = "Đến (dự kiến)")]
        [DataType(DataType.Time)]
        public TimeSpan? EstimatedArrival { get; set; }

        [Display(Name = "Đi (dự kiến)")]
        [DataType(DataType.Time)]
        public TimeSpan? EstimatedDeparture { get; set; }

        [StringLength(500)]
        [Display(Name = "Ghi chú")]
        public string? Note { get; set; }

        // Helper để chuyển đổi sang entity TripStop
        public TripStop ToTripStop(int tripId)
        {
            return new TripStop
            {
                StopId = this.StopId,
                TripId = tripId,
                StopOrder = this.StopOrder,
                StationName = this.StationName,
                Latitude = this.Latitude,
                Longitude = this.Longitude,
                EstimatedArrival = this.EstimatedArrival,
                EstimatedDeparture = this.EstimatedDeparture,
                Note = this.Note,
                // CreatedAt và UpdatedAt sẽ được EF Core hoặc DB xử lý
            };
        }
    }

}