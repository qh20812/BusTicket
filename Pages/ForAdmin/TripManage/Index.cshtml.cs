using System.ComponentModel.DataAnnotations;
using BusTicketSystem.Data;
using BusTicketSystem.Models;
using BusTicketSystem.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BusTicketSystem.Pages.ForAdmin.TripManage
{
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;
        public IndexModel(AppDbContext context)
        {
            _context = context;
        }

        public IList<TripViewModel> Trips { get; set; } = new List<TripViewModel>();
        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }
        [BindProperty(SupportsGet = true)]
        public TripStatus? FilterStatus { get; set; }
        [BindProperty(SupportsGet = true)]
        public string? SortOrder { get; set; }
        [BindProperty]
        public TripActionInputModel TripActionInput { get; set; } = new TripActionInputModel();

        // PendingApprovalRoutes and RouteActionInput moved to RouteManage
        private readonly TimeSpan _cancellationBuffer = TimeSpan.FromHours(2);

        public async Task OnGetAsync()
        {
            IQueryable<Trip> query = _context.Trips
                .Include(t => t.Route)
                .Include(t => t.Bus)
                .Include(t => t.Driver)
                .Include(t => t.Company);

            var tripsToAutoComplete = await query.Where(t => t.Status == TripStatus.Scheduled && t.DepartureTime.AddHours(24) < DateTime.UtcNow).ToListAsync();
            if (tripsToAutoComplete.Any())
            {
                foreach (var trip in tripsToAutoComplete)
                {
                    trip.Status = TripStatus.Completed;
                    trip.CompletedAt = DateTime.UtcNow;
                    trip.UpdatedAt = DateTime.UtcNow;
                }
                await _context.SaveChangesAsync();
            }
            if (!string.IsNullOrEmpty(SearchTerm))
            {
                query = query.Where(t => (t.Route != null && (t.Route.Departure.Contains(SearchTerm) || t.Route.Destination.Contains(SearchTerm))) ||
                    (t.Bus != null && t.Bus.LicensePlate.Contains(SearchTerm)) || (t.Driver != null && t.Driver.Fullname != null && t.Driver.Fullname.Contains(SearchTerm)));
            }
            if (FilterStatus.HasValue)
            {
                query = query.Where(t => t.Status == FilterStatus.Value);
            }
            switch (SortOrder?.ToLower())
            {
                case "departure_asc":
                    query = query.OrderBy(t => t.DepartureTime);
                    break;
                case "departure_desc":
                    query = query.OrderByDescending(t => t.DepartureTime);
                    break;
                case "price_asc":
                    query = query.OrderBy(t => t.Price);
                    break;
                case "price_desc":
                    query = query.OrderByDescending(t => t.Price);
                    break;
                case "status_asc":
                    query = query.OrderBy(t => t.Status);
                    break;
                case "status_desc":
                    query = query.OrderByDescending(t => t.Status);
                    break;
                default:
                    query = query.OrderByDescending(t => t.DepartureTime);
                    break;
            }
            var tripsData = await query.ToListAsync();
            Trips = tripsData.Select(t => new TripViewModel(t, _cancellationBuffer)).ToList();
        }
        public async Task<IActionResult> OnPostProcessActionAsync()
        {
            
            var routeActionKeysToRemove = ModelState.Keys.Where(k => k.StartsWith("RouteActionInput.")).ToList();
            foreach (var key in routeActionKeysToRemove)
            {
                if (ModelState.TryGetValue(key, out var modelStateEntry))
                {
                    modelStateEntry.Errors.Clear();
                    modelStateEntry.ValidationState = Microsoft.AspNetCore.Mvc.ModelBinding.ModelValidationState.Valid;
                }
                ModelState.Remove(key);
            }
            if (TripActionInput.ActionType == "Complete")
            {
                if (ModelState.ContainsKey("TripActionInput.CancellationReason"))
                {
                    ModelState.Remove("TripActionInput.CancellationReason");
                }
            }
            else if (TripActionInput.ActionType == "Cancel")
            {
                if (string.IsNullOrWhiteSpace(TripActionInput.CancellationReason))
                {
                    ModelState.AddModelError("TripActionInput.CancellationReason", "Lý do hủy không được để trống.");
                }
            }

            if (!ModelState.IsValid)
            {
                await OnGetAsync();
                TempData["ShowActionModalOnError"] = TripActionInput.TripIdAction.ToString();
                TempData["ActionTypeOnError"] = TripActionInput.ActionType;
                var tripActionErrors = ModelState.Where(kvp => kvp.Key.StartsWith("TripActionInput.")).SelectMany(kvp => kvp.Value.Errors).Select(e => e.ErrorMessage).ToList();
                if (tripActionErrors.Any())
                {
                    TempData["ActionError"] = string.Join("; ", tripActionErrors);

                }
                else
                {
                    var firstGeneralError = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage;
                    TempData["ActionError"] = firstGeneralError ?? "Dữ liệu không hợp lệ.";
                }
                return Page();
            }
            var trip = await _context.Trips
                .Include(t => t.Route)
                .Include(t => t.Tickets)
                .FirstOrDefaultAsync(t => t.TripId == TripActionInput.TripIdAction);
            if (trip == null) return NotFound();

            string successMessage = "";
            string notificationMessage = "";
            NotificationCategory notificationCategory = NotificationCategory.Trip;
            if (TripActionInput.ActionType == "Cancel")
            {
                if (trip.Status != TripStatus.Scheduled || trip.DepartureTime <= DateTime.UtcNow.Add(_cancellationBuffer))
                {
                    TempData["ErrorMessage"] = "Không thể hủy chuyến này (đã qua giờ cho phép hủy hoặc trạng thái không phù hợp).";
                    return RedirectToPage();
                }
                trip.Status = TripStatus.Cancelled;
                trip.CancellationReason = TripActionInput.CancellationReason;
                trip.UpdatedAt = DateTime.UtcNow;

                var bookedTickets = trip.Tickets.Where(ti => ti.Status == TicketStatus.Booked).ToList();
                foreach (var ticket in bookedTickets)
                {
                    ticket.Status = TicketStatus.Cancelled;
                    ticket.CancelledAt = DateTime.UtcNow;
                }
                successMessage = $"Đã hủy chuyến đi có ID {trip.TripId} ({trip.Route?.Departure} - {trip.Route?.Destination}).";
                notificationMessage = $"Chuyến đi ID {trip.TripId} ({trip.Route?.Departure} - {trip.Route?.Destination}) đã bị hủy. Lý do: {trip.CancellationReason}";
            }
            else if (TripActionInput.ActionType == "Complete")
            {
                if (trip.Status != TripStatus.Scheduled)
                {
                    TempData["ErrorMessage"] = "Chỉ có thể hoàn thành chuyến đi đang ở trạng thái 'Đã lên lịch'.";
                    return RedirectToPage();
                }
                trip.Status = TripStatus.Completed;
                trip.CompletedAt = DateTime.UtcNow;
                trip.UpdatedAt = DateTime.UtcNow;
                successMessage = $"Đã đánh dấu hoàn thành chuyến đi ID {trip.TripId} ({trip.Route?.Departure} - {trip.Route?.Destination}).";
                notificationMessage = $"Chuyến đi ID {trip.TripId} ({trip.Route?.Departure} - {trip.Route?.Destination}) đã được hoàn thành.";
            }
            else
            {
                TempData["ErrorMessage"] = "Hành động không hợp lệ hoặc không được chỉ định.";
                await OnGetAsync();
                return Page();
            }
            var notification = new Notification
            {
                Message = notificationMessage,
                Category = notificationCategory,
                TargetUrl = Url.Page("/ForAdmin/TripManage/Index", new { SearchTerm = trip.TripId.ToString() }),
                IconCssClass = (TripActionInput.ActionType == "Cancel" ? "bi bi-calendar-x" : "bi bi-calendar-check"),
                RecipientUserId = null // Reverted
            };
            _context.Notifications.Add(notification); // Thêm dòng này để lưu thông báo
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = successMessage;
            return RedirectToPage(new { SearchTerm, FilterStatus, SortOrder });
        }
        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var trip = await _context.Trips.Include(t => t.Tickets).FirstOrDefaultAsync(t => t.TripId == id);
            if (trip == null) return NotFound();
            if (trip.Tickets.Any(t => t.Status == TicketStatus.Booked || t.Status == TicketStatus.Used))
            {
                TempData["ErrorMessage"] = "Không thể xóa chuyến đi đã có vé được đặt hoặc đã sử dụng. Vui lòng hủy chuyến nếu cần.";
                return RedirectToPage();
            }
            if (trip.Status == TripStatus.Completed)
            {
                TempData["ErrorMessage"] = "Không thể xóa chuyến đi đã hoàn thành.";
                return RedirectToPage();
            }
            _context.Trips.Remove(trip);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Đã xóa chuyến đi ID {id} ({trip.Route?.Departure} - {trip.Route?.Destination}, khởi hành {trip.DepartureTime:dd/MM/yyyy HH:mm}) thành công.";
            return RedirectToPage(new { SearchTerm, FilterStatus, SortOrder });
        }
        public async Task<JsonResult> OnGetRouteDetailsForMapAsync(int tripId)
        {
            var trip = await _context.Trips
                .Include(t => t.Route)
                .FirstOrDefaultAsync(t => t.TripId == tripId);

            if (trip == null || trip.Route == null)
            {
                return new JsonResult(NotFound("Không tìm thấy thông tin chuyến đi hoặc lộ trình."));
            }

            return new JsonResult(new
            {
                originCoordinates = trip.Route.OriginCoordinates,
                destinationCoordinates = trip.Route.DestinationCoordinates,
                originAddress = trip.Route.OriginAddress ?? trip.Route.Departure,
                destinationAddress = trip.Route.DestinationAddress ?? trip.Route.Destination,
                distance = trip.Route.Distance,
                estimatedDuration = trip.Route.EstimatedDuration?.ToString(@"hh\h\ mm\m") 
            });
        }
    }
    public class TripActionInputModel
    {
        [Required]
        public int TripIdAction { get; set; }
        [StringLength(500, ErrorMessage = "Lý do không vượt quá 500 ký tự.")]
        public string? CancellationReason { get; set; }
        [Required]
        public string ActionType { get; set; } = string.Empty;
    }
}