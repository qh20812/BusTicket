using BusTicketSystem.Data;
using BusTicketSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;

namespace BusTicketSystem.Pages.ForAdmin.CancelManage
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;

        public IndexModel(AppDbContext context)
        {
            _context = context;
        }

        public IList<CancellationRequestViewModel> PendingCancellations { get; set; } = new List<CancellationRequestViewModel>();

        [BindProperty]
        public AdminActionInputModel AdminAction { get; set; } = new AdminActionInputModel();

        public async Task OnGetAsync()
        {
            PendingCancellations = await _context.Cancellations
                .Where(c => c.Status == CancellationRequestStatus.PendingApproval)
                .Include(c => c.Ticket)
                    .ThenInclude(t => t!.User)
                .Include(c => c.Ticket)
                    .ThenInclude(t => t!.Trip)
                        .ThenInclude(tr => tr!.Route)
                .Select(c => new CancellationRequestViewModel
                {
                    CancellationId = c.CancellationId,
                    TicketId = c.TicketId,
                    OrderId = c.OrderId,
                    CustomerName = c.Ticket!.User!.Fullname ?? c.Ticket.User.Username,
                    RouteName = c.Ticket.Trip!.Route!.Departure + " -> " + c.Ticket.Trip.Route.Destination,
                    DepartureTime = c.Ticket.Trip.DepartureTime,
                    SeatNumber = c.Ticket.SeatNumber,
                    CustomerReason = c.Reason,
                    RequestedAt = c.RequestedAt,
                    TicketPrice = c.Ticket.Price
                })
                .OrderBy(c => c.RequestedAt)
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostProcessRequestAsync()
        {
            if (!ModelState.IsValid)
            {
                await OnGetAsync(); // Reload data
                return Page();
            }

            var cancellation = await _context.Cancellations
                .Include(c => c.Ticket)
                    .ThenInclude(t => t!.User) // For notification
                .FirstOrDefaultAsync(c => c.CancellationId == AdminAction.CancellationId);

            if (cancellation == null || cancellation.Status != CancellationRequestStatus.PendingApproval)
            {
                TempData["ErrorMessage"] = "Không tìm thấy yêu cầu hủy vé hoặc yêu cầu đã được xử lý.";
                return RedirectToPage();
            }

            cancellation.ProcessedAt = DateTime.UtcNow;
            cancellation.AdminNotesOrRejectionReason = AdminAction.AdminNotes;
            // cancellation.ProcessedByAdminId = ... // Get current admin ID

            string userNotificationMessage;

            if (AdminAction.Action == "Approve")
            {
                cancellation.Status = CancellationRequestStatus.Approved;
                cancellation.Ticket!.Status = TicketStatus.Cancelled;
                cancellation.Ticket.CancelledAt = DateTime.UtcNow;
                cancellation.RefundedAmount = cancellation.Ticket.Price; // Basic refund logic
                // TODO: Add actual refund processing logic here if integrated with payment gateway

                userNotificationMessage = $"Yêu cầu hủy vé #{cancellation.TicketId} của bạn đã được duyệt. Số tiền {cancellation.RefundedAmount:N0} VND sẽ được hoàn lại.";
                TempData["SuccessMessage"] = $"Đã duyệt yêu cầu hủy vé #{cancellation.TicketId}.";
            }
            else // Reject
            {
                cancellation.Status = CancellationRequestStatus.Rejected;
                userNotificationMessage = $"Yêu cầu hủy vé #{cancellation.TicketId} của bạn đã bị từ chối. Lý do: {AdminAction.AdminNotes}";
                TempData["SuccessMessage"] = $"Đã từ chối yêu cầu hủy vé #{cancellation.TicketId}.";
            }

            // Notify customer
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            var customerNotification = new Notification
            {
                RecipientUserId = cancellation.Ticket.UserId,
                Message = userNotificationMessage,
                Category = NotificationCategory.Order,
                TargetUrl = Url.Page("/ForCustomer/MyOrders/Index"),
                IconCssClass = AdminAction.Action == "Approve" ? "bi bi-check-circle-fill" : "bi bi-x-circle-fill"
            };
#pragma warning restore CS8602 // Dereference of a possibly null reference.
            _context.Notifications.Add(customerNotification);

            await _context.SaveChangesAsync();
            return RedirectToPage();
        }
    }

    public class CancellationRequestViewModel
    {
        public int CancellationId { get; set; }
        public int TicketId { get; set; }
        public int OrderId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string RouteName { get; set; } = string.Empty;
        public DateTime DepartureTime { get; set; }
        public string SeatNumber { get; set; } = string.Empty;
        public string CustomerReason { get; set; } = string.Empty;
        public DateTime RequestedAt { get; set; }
        public decimal TicketPrice { get; set; }
    }

    public class AdminActionInputModel
    {
        [Required]
        public int CancellationId { get; set; }
        [Required(ErrorMessage = "Vui lòng chọn hành động.")]
        public string Action { get; set; } = string.Empty; // "Approve" or "Reject"
        [StringLength(500)]
        public string? AdminNotes { get; set; } // Required if rejecting
    }
}