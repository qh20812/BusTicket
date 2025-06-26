using BusTicketSystem.Data;
using BusTicketSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using BusTicketSystem.Pages.ForAdmin.TripManage;


namespace BusTicketSystem.Pages.ForPartner.TripManage
{
    [Authorize(Roles = "partner")]
    public class EditModel : PartnerBasePageModel // Inherit from PartnerBasePageModel
    {
        public EditModel(AppDbContext context, IHttpContextAccessor httpContextAccessor) : base(context, httpContextAccessor)
        {
        }

        [BindProperty]
        public TripInputModel TripInput { get; set; } = new TripInputModel();

        public SelectList Routes { get; set; } = new SelectList(Enumerable.Empty<SelectListItem>());
        public SelectList Buses { get; set; } = new SelectList(Enumerable.Empty<SelectListItem>());
        public SelectList Drivers { get; set; } = new SelectList(Enumerable.Empty<SelectListItem>());
        // No Companies SelectList needed for partner

        private async Task LoadSelectListsAsync(int? selectedRouteId = null, int? selectedBusId = null, int? selectedDriverId = null)
        {
            if (PartnerCompanyId == null) return; // Should be handled by base or earlier check

            // Routes: Partners can use approved routes or submit new ones.
            // For simplicity, let's assume they can select from all *approved* routes for now.
            // Or, routes they created that are approved.
            var partnerRoutes = await _context.Routes
                                .Where(r => r.Status == RouteStatus.Approved) // Only approved routes
                                .OrderBy(r => r.Departure)
                                .ThenBy(r => r.Destination)
                                .Select(r => new { r.RouteId, Name = $"{r.Departure} \u2192 {r.Destination} (ID: {r.RouteId})" })
                                .AsNoTracking()
                                .ToListAsync();
            Routes = new SelectList(partnerRoutes, "RouteId", "Name", selectedRouteId);

            var partnerBuses = await _context.Buses
                                .Where(b => b.CompanyId == PartnerCompanyId && b.Status == BusStatus.Active)
                                .OrderBy(b => b.LicensePlate)
                                .Select(b => new { b.BusId, Name = $"{b.LicensePlate} ({b.BusType} - {b.Capacity} ghế)" })
                                .AsNoTracking()
                                .ToListAsync();
            Buses = new SelectList(partnerBuses, "BusId", "Name", selectedBusId);

            var partnerDrivers = await _context.Drivers
                                     .Where(d => d.CompanyId == PartnerCompanyId && d.Status == DriverStatus.Active)
                                     .OrderBy(d => d.Fullname)
                                     .Select(d => new { d.DriverId, Name = $"{d.Fullname} (ID: {d.DriverId})" })
                                     .AsNoTracking()
                                     .ToListAsync();
            Drivers = new SelectList(partnerDrivers, "DriverId", "Name", selectedDriverId);
        }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (PartnerCompanyId == null)
            {
                TempData["ErrorMessage"] = "Không thể xác định thông tin nhà xe.";
                return RedirectToPage("/Index"); // Or partner dashboard
            }

            if (id == null) // Create new trip
            {
                ViewData["Title"] = "Đề xuất Chuyến đi mới";
                TripInput = new TripInputModel { DepartureTime = DateTime.Now.Date.AddDays(1).AddHours(8) };
                await LoadSelectListsAsync();
            }
            else // Edit existing trip (if allowed, e.g., if it's PendingApproval or Rejected)
            {
                ViewData["Title"] = "Chỉnh sửa Chuyến đi";
                var trip = await _context.Trips
                    .Include(t => t.TripStops.OrderBy(ts => ts.StopOrder))
                    .FirstOrDefaultAsync(t => t.TripId == id && t.CompanyId == PartnerCompanyId);

                if (trip == null) return NotFound("Không tìm thấy chuyến đi hoặc bạn không có quyền truy cập.");

                // Partners can only edit trips that are PendingApproval or Rejected.
                if (trip.Status != TripStatus.PendingApproval && trip.Status != TripStatus.Rejected)
                {
                    TempData["ErrorMessage"] = "Bạn chỉ có thể chỉnh sửa chuyến đi đang chờ duyệt hoặc bị từ chối.";
                    return RedirectToPage("./Index");
                }

#pragma warning disable CS8629 // Nullable value type may be null.
                TripInput = new TripInputModel
                {
                    TripId = trip.TripId,
                    RouteId = trip.RouteId,
                    BusId = trip.BusId,
                    DriverId = trip.DriverId,
                    // CompanyId is implicit
                    DepartureTime = trip.DepartureTime,
                    Price = trip.Price,
                    InitialAvailableSeats = trip.AvailableSeats.Value,
                    Status = trip.Status, // Display current status
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
#pragma warning restore CS8629 // Nullable value type may be null.
                await LoadSelectListsAsync(trip.RouteId, trip.BusId, trip.DriverId);
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (PartnerCompanyId == null) return Forbid();
            if (TripInput.DepartureTime <= DateTime.UtcNow.AddHours(1))
                ModelState.AddModelError("TripInput.DepartureTime", "Thời gian khởi hành phải sau ít nhất 1 giờ nữa.");
            bool busConflict = await _context.Trips.AnyAsync(t => t.BusId == TripInput.BusId && t.TripId != TripInput.TripId &&
                                                                t.CompanyId == PartnerCompanyId && // Ensure check is within partner's scope
                                                                (t.Status == TripStatus.Scheduled || t.Status == TripStatus.PendingApproval) &&
                                                                t.DepartureTime < TripInput.DepartureTime.AddHours(4) &&
                                                                t.DepartureTime.AddHours(4) > TripInput.DepartureTime);
            if (busConflict) ModelState.AddModelError("TripInput.BusId", "Xe này đã có lịch trình bị trùng thời gian.");
            if (!ModelState.IsValid)
            {
                await LoadSelectListsAsync(TripInput.RouteId, TripInput.BusId, TripInput.DriverId);
                ViewData["Title"] = TripInput.TripId == 0 ? "Đề xuất Chuyến đi mới" : "Chỉnh sửa Chuyến đi";
                TripInput.TripStops ??= new List<TripStopInputModel>();
                return Page();
            }

            string successMessage;
            string notificationMessageToAdmin;

            var bus = await _context.Buses.FirstOrDefaultAsync(b => b.BusId == TripInput.BusId && b.CompanyId == PartnerCompanyId);
            if (bus == null)
            {
                ModelState.AddModelError("TripInput.BusId", "Xe không hợp lệ hoặc không thuộc nhà xe của bạn.");
                await LoadSelectListsAsync(TripInput.RouteId, TripInput.BusId, TripInput.DriverId);
                return Page();
            }

            if (TripInput.TripId == 0) 
            {
#pragma warning disable CS8629 // Nullable value type may be null.
                var newTrip = new Trip
                {
                    RouteId = TripInput.RouteId.Value, // Trip.RouteId is int, TripInput.RouteId is int?
                    BusId = TripInput.BusId.Value,
                    DriverId = TripInput.DriverId.Value, // Assuming Trip.DriverId is seen as int here by compiler
                    CompanyId = PartnerCompanyId, 
                    DepartureTime = TripInput.DepartureTime,
                    Price = TripInput.Price,
                    AvailableSeats = bus.Capacity, 
                    Status = TripStatus.PendingApproval,
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
                successMessage = "Đã gửi đề xuất chuyến đi mới thành công. Chờ quản trị viên duyệt.";
                var routeInfo = await _context.Routes.FindAsync(newTrip.RouteId);
                notificationMessageToAdmin = $"Nhà xe '{PartnerCompanyName ?? PartnerCompanyId.ToString()}' đã đề xuất chuyến đi mới: {routeInfo?.Departure} - {routeInfo?.Destination}, khởi hành {newTrip.DepartureTime:g}.";
            }
            else // Editing an existing trip proposal (PendingApproval or Rejected)
            {
                var tripToUpdate = await _context.Trips.Include(t=>t.TripStops).FirstOrDefaultAsync(t => t.TripId == TripInput.TripId && t.CompanyId == PartnerCompanyId);
                if (tripToUpdate == null) return NotFound();

                if (tripToUpdate.Status != TripStatus.PendingApproval && tripToUpdate.Status != TripStatus.Rejected)
                {
                    TempData["ErrorMessage"] = "Không thể chỉnh sửa chuyến đi này.";
                    return RedirectToPage("./Index");
                }

#pragma warning disable CS8629 // Nullable value type may be null.
                tripToUpdate.RouteId = TripInput.RouteId.Value; // Trip.RouteId is int, TripInput.RouteId is int?
#pragma warning restore CS8629 // Nullable value type may be null.
#pragma warning disable CS8629 // Nullable value type may be null.
                tripToUpdate.BusId = TripInput.BusId.Value;
#pragma warning restore CS8629 // Nullable value type may be null.
#pragma warning disable CS8629 // Nullable value type may be null.
                tripToUpdate.DriverId = TripInput.DriverId.Value; // Assuming Trip.DriverId is seen as int here by compiler
#pragma warning restore CS8629 // Nullable value type may be null.
                tripToUpdate.DepartureTime = TripInput.DepartureTime;
                tripToUpdate.Price = TripInput.Price;
                tripToUpdate.AvailableSeats = bus.Capacity; // Recalculate based on bus
                tripToUpdate.Status = TripStatus.PendingApproval; // Resubmit as pending
                tripToUpdate.UpdatedAt = DateTime.UtcNow;
                tripToUpdate.CancellationReason = null; // Clear rejection reason if any

                // Update TripStops (similar to admin's logic: remove old, add/update new)
                _context.TripStops.RemoveRange(tripToUpdate.TripStops);
                if (TripInput.TripStops != null)
                {
                    foreach (var stopInput in TripInput.TripStops)
                    {
                        _context.TripStops.Add(stopInput.ToTripStop(tripToUpdate.TripId));
                    }
                }

                _context.Attach(tripToUpdate).State = EntityState.Modified;
                successMessage = "Đã cập nhật và gửi lại đề xuất chuyến đi. Chờ quản trị viên duyệt.";
                var routeInfo = await _context.Routes.FindAsync(tripToUpdate.RouteId);
                notificationMessageToAdmin = $"Nhà xe '{PartnerCompanyName ?? PartnerCompanyId.ToString()}' đã cập nhật đề xuất chuyến đi: {routeInfo?.Departure} - {routeInfo?.Destination}, khởi hành {tripToUpdate.DepartureTime:g}.";
            }

            await _context.SaveChangesAsync();

            // Create notification for Admins
            var adminNotification = new BusTicketSystem.Models.Notification
            {
                Message = notificationMessageToAdmin,
                Category = NotificationCategory.Trip, // Or a new "PartnerSubmission" category
                TargetUrl = $"/ForAdmin/TripManage/Edit/{ (TripInput.TripId == 0 ? _context.Trips.Local.FirstOrDefault()?.TripId : TripInput.TripId)}", // URL for admin to review
                IconCssClass = "bi bi-journal-arrow-up",
                // RecipientUserId = null; // For all admins, or target specific admin roles
            };
            _context.Notifications.Add(adminNotification);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = successMessage;
            return RedirectToPage("./Index");
        }

        // OnGetRouteDetailsAsync can be similar to Admin's, fetching approved route details.
        public async Task<JsonResult> OnGetRouteDetailsAsync(int routeId)
        {
            var route = await _context.Routes.FirstOrDefaultAsync(r => r.RouteId == routeId && r.Status == RouteStatus.Approved);
            if (route == null) return new JsonResult(NotFound("Lộ trình không tìm thấy hoặc chưa được duyệt.")) { StatusCode = 404 };
            // ... return route details ...
            return new JsonResult(new { /* route data */ });
        }
    }
}