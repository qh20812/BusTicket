using BusTicketSystem.Data;
using BusTicketSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using BusTicketSystem.Helpers;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;

namespace BusTicketSystem.Pages.ForCustomer.MyOrders
{
    [Authorize(Roles = "Member")]
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private static readonly TimeSpan CancellationCutoff = TimeSpan.FromHours(2); // Allow cancellation up to 2 hours before departure

        public IndexModel(AppDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public List<TicketViewModel> UpcomingTickets { get; set; } = new List<TicketViewModel>();
        public List<TicketViewModel> CompletedTickets { get; set; } = new List<TicketViewModel>();
        public List<TicketViewModel> CancelledTickets { get; set; } = new List<TicketViewModel>();

        [BindProperty]
        public CancellationInputModel CancellationInput { get; set; } = new CancellationInputModel();

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = _httpContextAccessor.HttpContext?.Session.GetUserId();
            if (userId == null)
            {
                return RedirectToPage("/Account/Login/Login", new { area = "" });
            }

            var tickets = await _context.Tickets
                .Where(t => t.UserId == userId)
                .Include(t => t.Trip)
                    .ThenInclude(tr => tr!.Route)
                .Include(t => t.Order)
                .Include(t => t.Cancellations) // Include cancellations to check status
                .OrderByDescending(t => t.Trip!.DepartureTime)
                .ToListAsync();

            foreach (var ticket in tickets)
            {
                var ticketVm = new TicketViewModel(ticket, CancellationCutoff);

                // Check if there's an active cancellation request
                var pendingCancellation = ticket.Cancellations
                    .FirstOrDefault(c => c.Status == CancellationRequestStatus.PendingApproval);
                ticketVm.IsCancellationPending = pendingCancellation != null;

                if (ticket.Status == TicketStatus.Cancelled || ticket.Cancellations.Any(c => c.Status == CancellationRequestStatus.Approved))
                {
                    CancelledTickets.Add(ticketVm);
                }
                else if (ticket.Status == TicketStatus.Used || (ticket.Trip!.Status == TripStatus.Completed && ticket.Status == TicketStatus.Booked))
                {
                    CompletedTickets.Add(ticketVm);
                }
                // Sử dụng DateTime.Now để so sánh nếu DepartureTime lưu theo giờ Việt Nam
                else if (ticket.Status == TicketStatus.Booked && ticket.Trip!.Status == TripStatus.Scheduled && ticket.Trip.DepartureTime > DateTime.Now)
                {
                    UpcomingTickets.Add(ticketVm);
                }
            }

            return Page();
        }

        public async Task<IActionResult> OnPostRequestCancellationAsync()
        {
            var userId = _httpContextAccessor.HttpContext?.Session.GetUserId();
            if (userId == null)
            {
                return RedirectToPage("/Account/Login/Login");
            }

            if (!ModelState.IsValid)
            {
                await OnGetAsync(); // Reload ticket lists
                TempData["ErrorMessage"] = "Lý do hủy vé không hợp lệ.";
                return Page();
            }

            var ticket = await _context.Tickets
                .Include(t => t.Trip)
                .Include(t => t.Cancellations)
                .FirstOrDefaultAsync(t => t.TicketId == CancellationInput.TicketId && t.UserId == userId);

            if (ticket == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy vé hoặc vé không thuộc về bạn.";
                return RedirectToPage("./Index");
            }

            if (ticket.Trip!.DepartureTime <= DateTime.UtcNow.Add(CancellationCutoff))
            {
                TempData["ErrorMessage"] = "Đã quá thời gian cho phép hủy vé này.";
                return RedirectToPage("./Index");
            }

            if (ticket.Status != TicketStatus.Booked)
            {
                TempData["ErrorMessage"] = "Chỉ có thể hủy vé đang ở trạng thái 'Đã đặt'.";
                return RedirectToPage("./Index");
            }

            if (ticket.Cancellations.Any(c => c.Status == CancellationRequestStatus.PendingApproval || c.Status == CancellationRequestStatus.Approved))
            {
                TempData["InfoMessage"] = "Yêu cầu hủy cho vé này đã tồn tại hoặc đã được xử lý.";
                return RedirectToPage("./Index");
            }

            var cancellation = new Cancellation
            {
                TicketId = ticket.TicketId,
                OrderId = ticket.OrderId,
                Reason = CancellationInput.Reason,
                RequestedAt = DateTime.UtcNow,
                Status = CancellationRequestStatus.PendingApproval
            };

            _context.Cancellations.Add(cancellation);

            // Create notification for admin
            var adminNotification = new Notification
            {
                Message = $"Khách hàng '{ticket.User?.Username ?? "N/A"}' (ID: {userId}) yêu cầu hủy vé #{ticket.TicketId} cho chuyến đi lúc {ticket.Trip.DepartureTime:dd/MM/yyyy HH:mm}.",
                Category = NotificationCategory.Order, // Or a new "CancellationRequest" category
                TargetUrl = Url.Page("/ForAdmin/OrderManage/Index"), // Link to admin order management page (which now includes cancellations)
                IconCssClass = "bi bi-journal-x",
                // RecipientUserId = null, // For all admins, or target specific admin role
            };
            _context.Notifications.Add(adminNotification);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Yêu cầu hủy vé của bạn đã được gửi. Vui lòng chờ quản trị viên duyệt.";
            return RedirectToPage("./Index");
        }
    }

    public class TicketViewModel
    {
        public int TicketId { get; set; }
        public string RouteName { get; set; } = string.Empty;
        public DateTime DepartureTime { get; set; }
        public string SeatNumber { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public TicketStatus Status { get; set; }
        public string StatusDisplayName => Status.ToString(); // Add DisplayName attribute to enum if needed
        public bool CanCancel { get; set; }
        public bool IsCancellationPending { get; set; }
        public int OrderId { get; set; }

        public TicketViewModel(Ticket ticket, TimeSpan cancellationCutoff)
        {
            TicketId = ticket.TicketId;
            RouteName = ticket.Trip?.Route != null ? $"{ticket.Trip.Route.Departure} \u2192 {ticket.Trip.Route.Destination}" : "N/A";
            DepartureTime = ticket.Trip?.DepartureTime ?? DateTime.MinValue;
            SeatNumber = ticket.SeatNumber;
            Price = ticket.Price;
            Status = ticket.Status;
            OrderId = ticket.OrderId;
            CanCancel = ticket.Status == TicketStatus.Booked &&
                        ticket.Trip?.Status == TripStatus.Scheduled &&
                        DepartureTime > DateTime.UtcNow.Add(cancellationCutoff);
        }
    }

    public class CancellationInputModel
    {
        [Required]
        public int TicketId { get; set; }

        [Required(ErrorMessage = "Lý do hủy vé không được để trống.")]
        [StringLength(500, MinimumLength = 10, ErrorMessage = "Lý do hủy vé phải từ 10 đến 500 ký tự.")]
        public string Reason { get; set; } = string.Empty;
    }
}