using BusTicketSystem.Data;
using BusTicketSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace BusTicketSystem.Pages.ForAdmin.OrderManage
{
    [Authorize(Roles = "Admin")]
    public class IndexModel(AppDbContext context) : PageModel
    {
        private readonly AppDbContext _context = context;

        public IList<OrderViewModel> OrdersDisplay { get; set; } = new List<OrderViewModel>();

        // Properties for Cancellation Requests
        public IList<CancellationRequestViewModel> PendingCancellations { get; set; } = new List<CancellationRequestViewModel>();

        [BindProperty]
        public CancellationActionInputModel CancellationAction { get; set; } = new CancellationActionInputModel();

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public OrderStatus? FilterStatus { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? SortOrder { get; set; } // Ví dụ: "date_desc", "date_asc", "total_desc", "total_asc"

        public required SelectList Statuses { get; set; }

        public async Task OnGetAsync()
        {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            IQueryable<Order> query = _context.Orders
                .Include(o => o.User)
                .Include(o => o.Promotion)
                .Include(o => o.OrderTickets)
                    .ThenInclude(ot => ot.Ticket)
                        .ThenInclude(t => t.Trip)
                            .ThenInclude(tr => tr.Route);
#pragma warning restore CS8602 // Dereference of a possibly null reference.

            if (!string.IsNullOrEmpty(SearchTerm))
            {

#pragma warning disable CS8602 // Dereference of a possibly null reference.
                query = query.Where(o =>
                                o.OrderId.ToString().Contains(SearchTerm) ||
                                (o.User != null && (o.User.Fullname.Contains(SearchTerm) || o.User.Email.Contains(SearchTerm))) ||
                                (o.GuestEmail != null && o.GuestEmail.Contains(SearchTerm)) ||
                                (o.OrderTickets.Any(ot => ot.TicketId.ToString().Contains(SearchTerm)))
                                );
#pragma warning restore CS8602 // Dereference of a possibly null reference.
            }

            if (FilterStatus.HasValue)
            {
                query = query.Where(o => o.Status == FilterStatus.Value);
            }

            switch (SortOrder?.ToLower())
            {
                case "date_asc":
                    query = query.OrderBy(o => o.CreatedAt);
                    break;
                case "total_asc":
                    query = query.OrderBy(o => o.TotalAmount);
                    break;
                case "total_desc":
                    query = query.OrderByDescending(o => o.TotalAmount);
                    break;
                default: // "date_desc" hoặc mặc định
                    query = query.OrderByDescending(o => o.CreatedAt);
                    break;
            }

            var orders = await query.ToListAsync();
            OrdersDisplay = orders.Select(o => new OrderViewModel(o)).ToList();

            var enumValues = System.Enum.GetValues(typeof(OrderStatus)).Cast<OrderStatus>();
            Statuses = new SelectList(enumValues.Select(e => new SelectListItem
            {
                Value = e.ToString(),
                Text = OrderViewModel.GetStatusDisplayName(e) // Sử dụng helper để lấy tên tiếng Việt
            }), "Value", "Text", FilterStatus?.ToString());

            // Load Pending Cancellations
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

        public async Task<IActionResult> OnPostProcessCancellationAsync()
        {
            // Thêm logic kiểm tra điều kiện cho AdminNotes ở đây
            if (CancellationAction.Action == "Reject" && string.IsNullOrWhiteSpace(CancellationAction.AdminNotes))
            {
                ModelState.AddModelError("CancellationAction.AdminNotes", "Lý do từ chối không được để trống khi từ chối yêu cầu.");
                TempData["ActionOnError"] = "Reject"; // Thêm dòng này để JS biết mở lại modal
            }

            if (!ModelState.IsValid)
            {
                await OnGetAsync(); // Reload data for both orders and cancellations
                TempData["ShowCancellationModalOnError"] = CancellationAction.CancellationId.ToString(); // To reopen modal
                return Page();
            }

            var cancellation = await _context.Cancellations
                .Include(c => c.Ticket)
                    .ThenInclude(t => t!.User)
                .FirstOrDefaultAsync(c => c.CancellationId == CancellationAction.CancellationId);

            if (cancellation == null || cancellation.Status != CancellationRequestStatus.PendingApproval)
            {
                TempData["ErrorMessage"] = "Không tìm thấy yêu cầu hủy vé hoặc yêu cầu đã được xử lý.";
                return RedirectToPage();
            }

            cancellation.ProcessedAt = DateTime.UtcNow;
            cancellation.AdminNotesOrRejectionReason = CancellationAction.AdminNotes;
            // cancellation.ProcessedByAdminId = ... // Get current admin ID from session/claims

            string userNotificationMessage;
            string adminNotificationMessage;

            if (CancellationAction.Action == "Approve")
            {
                cancellation.Status = CancellationRequestStatus.Approved;
                cancellation.Ticket!.Status = TicketStatus.Cancelled;
                cancellation.Ticket.CancelledAt = DateTime.UtcNow;
                cancellation.RefundedAmount = cancellation.Ticket.Price; // Basic refund logic
                // TODO: Add actual refund processing logic here if integrated with payment gateway

                userNotificationMessage = $"Yêu cầu hủy vé #{cancellation.TicketId} của bạn đã được duyệt. Số tiền {cancellation.RefundedAmount:N0} VND sẽ được hoàn lại.";
                adminNotificationMessage = $"Đã duyệt yêu cầu hủy vé #{cancellation.TicketId} cho khách '{cancellation.Ticket.User?.Username}'.";
                TempData["SuccessMessage"] = $"Đã duyệt yêu cầu hủy vé #{cancellation.TicketId}.";
            }
            else // Reject
            {
                cancellation.Status = CancellationRequestStatus.Rejected;
                userNotificationMessage = $"Yêu cầu hủy vé #{cancellation.TicketId} của bạn đã bị từ chối. Lý do: {CancellationAction.AdminNotes}";
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                adminNotificationMessage = $"Đã từ chối yêu cầu hủy vé #{cancellation.TicketId} cho khách '{cancellation.Ticket.User?.Username}'. Lý do: {CancellationAction.AdminNotes}";
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                TempData["SuccessMessage"] = $"Đã từ chối yêu cầu hủy vé #{cancellation.TicketId}.";
            }

            // Notify customer
            var customerNotification = new Notification
            {
                RecipientUserId = cancellation.Ticket.UserId,
                Message = userNotificationMessage,
                Category = NotificationCategory.Order,
                TargetUrl = Url.Page("/ForCustomer/MyOrders/Index"),
                IconCssClass = CancellationAction.Action == "Approve" ? "bi bi-check-circle-fill" : "bi bi-x-circle-fill"
            };
            _context.Notifications.Add(customerNotification);

            // Optional: Notify admin (e.g., for logging or if other admins need to know)
            // var adminGeneralNotification = new Notification { Message = adminNotificationMessage, Category = NotificationCategory.Order, TargetUrl = Url.Page("./Index", new { SearchTerm = cancellation.OrderId.ToString() }) };
            // _context.Notifications.Add(adminGeneralNotification);

            await _context.SaveChangesAsync();
            return RedirectToPage();
        }
    }

    public class OrderViewModel
    {
        public int OrderId { get; set; }
        public string? CustomerName { get; set; }
        public string? CustomerEmail { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string TotalAmountDisplay => TotalAmount.ToString("N0") + " VND";
        public OrderStatus Status { get; set; }
        public string StatusDisplayName { get; set; }
        public string StatusCssClass { get; set; }
        public string? PaymentMethod { get; set; }
        public int TicketCount { get; set; }
        public string? FirstTripInfo { get; set; }
        public string? PromotionCode { get; set; }

        public OrderViewModel(Order order)
        {
            OrderId = order.OrderId;
            CustomerName = order.User?.Fullname ?? order.GuestEmail ?? "Khách vãng lai";
            CustomerEmail = order.User?.Email ?? order.GuestEmail;
            OrderDate = order.CreatedAt;
            TotalAmount = order.TotalAmount;
            Status = order.Status;
            StatusDisplayName = GetStatusDisplayName(order.Status);
            StatusCssClass = GetStatusCssClass(order.Status);
            PaymentMethod = order.PaymentMethod;
            PromotionCode = order.Promotion?.Code;

            if (order.OrderTickets != null && order.OrderTickets.Any())
            {
                TicketCount = order.OrderTickets.Count;
                var firstTicket = order.OrderTickets.FirstOrDefault()?.Ticket;
                if (firstTicket?.Trip?.Route != null)
                {
                    FirstTripInfo = $"{firstTicket.Trip.Route.Departure} \u2192 {firstTicket.Trip.Route.Destination} ({firstTicket.Trip.DepartureTime:dd/MM/yyyy HH:mm})";
                }
                else
                {
                    FirstTripInfo = "N/A";
                }
            }
            else
            {
                TicketCount = 0;
                FirstTripInfo = "N/A";
            }
        }

        public static string GetStatusDisplayName(OrderStatus status)
        {
            var field = typeof(OrderStatus).GetField(status.ToString());
            var displayAttribute = field?.GetCustomAttributes(typeof(DisplayAttribute), false).FirstOrDefault() as DisplayAttribute;
#pragma warning disable CS8603 // Possible null reference return.
            if (displayAttribute != null) return displayAttribute.GetName();
#pragma warning restore CS8603 // Possible null reference return.

            return status switch // Fallback nếu không có DisplayAttribute
            {
                OrderStatus.Pending => "Đang chờ",
                OrderStatus.Paid => "Đã thanh toán",
                OrderStatus.Cancelled => "Đã hủy",
                OrderStatus.Refunded => "Đã hoàn tiền",
                _ => status.ToString(),
            };
        }
        private static string GetStatusCssClass(OrderStatus status)
        {
            return status.ToString().ToLower();
        }
    }

    // ViewModel for displaying cancellation requests (moved from CancelTicket/Index.cshtml.cs)
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

    // InputModel for admin actions on cancellations (moved from CancelTicket/Index.cshtml.cs)
    public class CancellationActionInputModel
    {
        [Required]
        public int CancellationId { get; set; }
        [Required(ErrorMessage = "Vui lòng chọn hành động.")]
        public string Action { get; set; } = string.Empty; // "Approve" or "Reject"
        [StringLength(500)]
        // [RequiredIf("Action", "Reject", ErrorMessage = "Lý do từ chối không được để trống.")] // Đã xóa attribute này
        public string? AdminNotes { get; set; }
    }
}