using BusTicketSystem.Data;
using BusTicketSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using QRCoder;
using System.IO;

namespace BusTicketSystem.Pages.ForCustomer.Payment
{
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly ILogger<IndexModel> _logger;
        public IndexModel(AppDbContext context, ILogger<IndexModel> logger)
        {
            _context = context;
            _logger = logger;
        }
        [BindProperty(SupportsGet = true)]
        public int OrderId { get; set; }
        public decimal AmountToPay { get; set; }
        public string? QrDataUrl { get; set; }
        public string? ConfirmationUrl { get; set; }
        [TempData]
        public string ErrorMessage { get; set; }
        [TempData]
        public string SuccessMessage { get; set; }
        [TempData]
        public string InfoMessage { get; set; }
        public string? TicketRouteName { get; set; }
        public DateTime? TicketDepartureTime { get; set; }
        public string? TicketSeatNumbers { get; set; }
        public string? TicketOriginAddress { get; set; }
        public string? TicketDestinationAddress { get; set; }
        public decimal? TicketIndividualPrice { get; set; }
        public string? TicketBusLicensePlate { get; set; }
        public TicketType? TicketType { get; set; }

        public async Task<IActionResult> OnGetAsync(int? orderId)
        {
            if (orderId == null)
            {
                ErrorMessage = "Thiếu mã đơn hàng để tiến hành thanh toán.";
                return RedirectToPage("/ForCustomer/TicketInvoice/Index");
            }
            OrderId = orderId.Value;
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            var order = await _context.Orders
                .Include(o => o.OrderTickets)
                    .ThenInclude(ot => ot.Ticket)
                        .ThenInclude(t => t.Trip) 
                            .ThenInclude(trip => trip.Route) 
                .Include(o => o.OrderTickets)
                    .ThenInclude(ot => ot.Ticket)
                        .ThenInclude(t => t.Trip)
                            .ThenInclude(trip => trip.Bus) 
                .FirstOrDefaultAsync(o => o.OrderId == OrderId);
#pragma warning restore CS8602 // Dereference of a possibly null reference.
            if (order == null)
            {
                ErrorMessage = $"Không tìm thấy đơn hàng với ID {OrderId}.";
                return RedirectToPage("/ForCustomer/TicketInvoice/Index");
            }

            // Kiểm tra và tự động hủy đơn hàng nếu quá hạn 1 tiếng và đang chờ thanh toán
            if (order.Status == OrderStatus.Pending && order.CreatedAt.AddHours(1) < DateTime.UtcNow)
            {
                var ticketsToCancel = await _context.Tickets
                    .Where(t => t.OrderId == order.OrderId && t.Status == TicketStatus.Booked)
                    .ToListAsync();

                var affectedTripIds = new HashSet<int>();

                if (ticketsToCancel.Any())
                {
                    foreach (var ticket in ticketsToCancel)
                    {
                        ticket.Status = TicketStatus.Cancelled;
                        ticket.CancelledAt = DateTime.UtcNow;
                        _context.Tickets.Update(ticket);
                        if (ticket.TripId != 0)
                        {
                            affectedTripIds.Add(ticket.TripId);
                        }
                    }
                }

                order.Status = OrderStatus.Cancelled;
                order.UpdatedAt = DateTime.UtcNow;
                _context.Orders.Update(order);

                await _context.SaveChangesAsync(); // Lưu thay đổi cho Order và Tickets trước

                // Cập nhật lại AvailableSeats cho các Trip bị ảnh hưởng
                foreach (var tripId in affectedTripIds)
                {
                    var tripToUpdate = await _context.Trips.Include(t => t.Bus).FirstOrDefaultAsync(t => t.TripId == tripId);
                    if (tripToUpdate != null && tripToUpdate.Bus != null)
                    {
                        var currentBookedOrUsedSeats = await _context.Tickets
                            .CountAsync(t => t.TripId == tripToUpdate.TripId && (t.Status == TicketStatus.Booked || t.Status == TicketStatus.Used));
                        tripToUpdate.AvailableSeats = (tripToUpdate.Bus.Capacity ?? 0) - currentBookedOrUsedSeats;
                        _context.Trips.Update(tripToUpdate);
                    }
                }
                await _context.SaveChangesAsync(); // Lưu thay đổi cho Trips

                TempData["InfoMessage"] = "Đơn hàng đã tự động bị hủy do quá hạn thanh toán (sau 1 giờ).";
                return RedirectToPage("/ForCustomer/TicketInvoice/Index", new { orderId = OrderId });
            }

            if (order.Status != OrderStatus.Pending)
            {
                InfoMessage = $"Đơn hàng {OrderId} không ở trạng thái chờ thanh toán (Trạng thái hiện tại: {order.StatusVietnamese}).";
                return RedirectToPage("/ForCustomer/TicketInvoice/Index", new { orderId = OrderId }); // Đã có TempData, không cần SuccessMessage ở đây
            }
            if (order.OrderTickets != null && order.OrderTickets.Any())
            {
                var firstTicketInfo = order.OrderTickets.FirstOrDefault()?.Ticket;
                if (firstTicketInfo?.Trip != null)
                {
                    if (firstTicketInfo.Trip.Route != null)
                    {
                        TicketRouteName = $"{firstTicketInfo.Trip.Route.Departure} - {firstTicketInfo.Trip.Route.Destination}";
                        TicketOriginAddress = firstTicketInfo.Trip.Route.OriginAddress;
                        TicketDestinationAddress = firstTicketInfo.Trip.Route.DestinationAddress;
                    }
                    TicketDepartureTime = firstTicketInfo.Trip.DepartureTime;
                    TicketBusLicensePlate = firstTicketInfo.Trip.Bus?.LicensePlate ?? "Đang cập nhật";
                }
                TicketIndividualPrice = firstTicketInfo?.Price;
                TicketSeatNumbers = string.Join(", ", order.OrderTickets.Select(ot => ot.Ticket?.SeatNumber).Where(sn => !string.IsNullOrEmpty(sn)));
                TicketType = firstTicketInfo?.Type;
            }
            AmountToPay = order.TotalAmount;
            ConfirmationUrl = Url.Page("./Index", "ConfirmDemoPayment", new { orderId = OrderId });
            QrDataUrl = ConfirmationUrl;
            return Page();
        }
        public IActionResult OnGetGenerateQrCode(string data)
        {
            if (string.IsNullOrEmpty(data))
            {
                _logger.LogWarning("Attempted to generate QR code with empty data.");
                return BadRequest("Dữ liệu QR không được trống.");
            }
            QRCodeGenerator qRCodeGenerator = new QRCodeGenerator();
            QRCodeData qRCodeData = qRCodeGenerator.CreateQrCode(data, QRCodeGenerator.ECCLevel.Q);
            PngByteQRCode qRCode = new PngByteQRCode(qRCodeData);
            byte[] qrCodeAsPngByteArr = qRCode.GetGraphic(20);
            return File(qrCodeAsPngByteArr, "image/png");
        }
        public async Task<IActionResult> OnGetConfirmDemoPaymentAsync(int orderId)
        {
            _logger.LogInformation($"Xác nhận thanh toán demo cho Đơn hàng ID: {orderId}");
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null)
            {
                _logger.LogWarning($"Đơn hàng ID: {orderId} không tìm thấy để xác nhận thanh toán.");
                return new JsonResult(new { success = false, message = "Đơn hàng không tồn tại." }) { StatusCode = 404 };
            }
            if (order.Status == OrderStatus.Pending)
            {
                order.Status = OrderStatus.Paid;
                order.UpdatedAt = DateTime.UtcNow;
                _context.Orders.Update(order);

                // Tạo bản ghi Payment mới với PaymentGateway ngẫu nhiên
                var paymentGateways = Enum.GetValues(typeof(PaymentGateway)).Cast<PaymentGateway>().ToList();
                var random = new Random();
                var randomGateway = paymentGateways[random.Next(paymentGateways.Count)];

                var newPayment = new Models.Payment
                {
                    OrderId = order.OrderId,
                    TransactionId = $"DEMO_{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}", // ID giao dịch demo
                    PaymentGateway = randomGateway,
                    Amount = order.TotalAmount,
                    Status = PaymentStatus.Success,
                    TransactionTime = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    PaymentContent = $"Thanh toán demo cho đơn hàng {order.OrderId}"
                };
                _context.Payments.Add(newPayment);

                var ticketsToUpdate = await _context.Tickets.Where(t => t.OrderTickets.Any(ot => ot.OrderId == orderId)).ToListAsync();

                foreach (var ticket in ticketsToUpdate)
                {
                    ticket.Status = TicketStatus.Booked;
                }
                _context.Tickets.UpdateRange(ticketsToUpdate);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Đơn hàng ID: {orderId} đã xác nhận thanh toán thành công. Trạng thái mới: {order.Status}.");
                return new JsonResult(new 
                {
                    success = true,
                    message = "Thanh toán thành công!",
                    newStatus = order.StatusVietnamese,
                    redirectUrl = Url.Page("/ForCustomer/TicketInvoice/Index", new { orderId = orderId })
                });
            }
            else
            {
                _logger.LogInformation($"Đơn hàng ID: {orderId} không ở trạng thái Chờ thanh toán. Trạng thái hiện tại: {order.Status}.");
                return new JsonResult(new
                {
                    success = false,
                    message = $"Đơn hàng không ở trạng thái chờ thanh toán (Trạng thái hiện tại: {order.StatusVietnamese})."
                })
                { StatusCode = 400 };
            }
        }
    }
}