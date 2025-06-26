using System.ComponentModel.DataAnnotations;
using BusTicketSystem.Data;
using BusTicketSystem.Models;
using BusTicketSystem.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BusTicketSystem.Pages.Home.Schedule
{
    public class DetailsModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly TimeSpan _cancellationBuffer = TimeSpan.FromHours(2);
        public DetailsModel(AppDbContext context)
        {
            _context = context;
        }
        public TripViewModel? TripDetail { get; set; }
        [BindProperty(SupportsGet = true)]
        [Range(1, 5, ErrorMessage = "Số lượng vé không hợp lệ.")]
        public int Quantity { get; set; } = 1;
        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var trip = await _context.Trips
                .Include(t => t.Route)
                .Include(t => t.Bus)
                .Include(t => t.Company)
                .FirstOrDefaultAsync(t => t.TripId == id && t.Status == TripStatus.Scheduled);
            if (trip == null)
            {
                return NotFound();
            }
            TripDetail = new TripViewModel(trip, _cancellationBuffer);
            if (TripDetail.AvailableSeats < Quantity)
            {
                // hiển thị thông báo và cho người dùng chọn lại

            }
            return Page();
        }
        public async Task<JsonResult> OnGetSeatDataAsync(int tripId)
        {
            var trip = await _context.Trips.Include(t => t.Bus)
                .Include(t => t.Tickets.Where(ti => ti.Status == TicketStatus.Booked || ti.Status == TicketStatus.Used))
                .FirstOrDefaultAsync(t => t.TripId == tripId);
            if (trip == null || trip.Bus == null)
            {
                return new JsonResult(NotFound("Không tìm thấy chuyến đi hoặc xe."));
            }
            var bookedSeats = trip.Tickets.Select(t => t.SeatNumber).ToList();
            return new JsonResult(new { capacity = trip.Bus.Capacity, bookedSeats = bookedSeats });
        }
        public async Task<IActionResult> OnPostBookAsync([FromBody] BookingRequest bookingRequest)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return new JsonResult(new { success = false, message = "Dữ liệu không hợp lệ.", errors = errors }) { StatusCode = 400 };
            }
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    var trip = await _context.Trips
                        .Include(t => t.Bus)
                        .FirstOrDefaultAsync(t => t.TripId == bookingRequest.TripId);
                    if (trip == null || trip.Bus == null)
                    {
                        return new JsonResult(new { success = false, message = "Số lượng ghế đã chọn không khớp với số lượng yêu cầu." }) { StatusCode = 400 };
                    }
                    var alreadyBookedSeatNumbers = await _context.Tickets
                        .Where(t => t.TripId == bookingRequest.TripId && (t.Status == TicketStatus.Booked || t.Status == TicketStatus.Used) && bookingRequest.SelectedSeats.Contains(t.SeatNumber)).Select(t => t.SeatNumber).ToListAsync();
                    if (alreadyBookedSeatNumbers.Any())
                    {
                        return new JsonResult(new { success = false, message = $"Các ghế sau đã được bạn đặt trong lúc bạn thao tác: {string.Join<string>(", ",alreadyBookedSeatNumbers)}. Vui lòng chọn lại." }) { StatusCode = 400 };
                    }
                    int totalBookedSeatsForThisTrip = await _context.Tickets
                        .CountAsync(t => t.TripId == trip.TripId && (t.Status == TicketStatus.Booked || t.Status == TicketStatus.Used));
                    int actualAvailableSeats = (trip.Bus.Capacity ?? 0) - totalBookedSeatsForThisTrip;
                    if (bookingRequest.SelectedSeats.Count > actualAvailableSeats)
                    {
                        return new JsonResult(new { success = false, message = $"Không đủ ghế trống.Hiện còn {actualAvailableSeats} ghế, bạn yêu cầu {bookingRequest.SelectedSeats.Count} ghế." }) { StatusCode = 400 };
                    }
                    var order = new Order
                    {
                        UserId = null,
                        GuestPhone = bookingRequest.GuestPhone,
                        TotalAmount = bookingRequest.SelectedSeats.Count * trip.Price,
                        Status = OrderStatus.Pending,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        OrderTickets = new List<OrderTicket>()
                    };
                    _context.Add(order);
                    await _context.SaveChangesAsync();
                    var ticketsToCreate = new List<Ticket>();
                    var orderTicketsToCreate = new List<OrderTicket>();

                    foreach (var seatNumber in bookingRequest.SelectedSeats)
                    {
                        var ticket = new Ticket
                        {
                            TripId = trip.TripId,
                            OrderId = order.OrderId,
                            UserId = order.UserId,
                            SeatNumber = seatNumber,
                            Price = trip.Price,
                            Status = TicketStatus.Booked,
                            BookedAt = DateTime.UtcNow
                        };
                        ticketsToCreate.Add(ticket);
                    }
                    _context.Tickets.AddRange(ticketsToCreate);
                    await _context.SaveChangesAsync();
                    foreach (var createdTicket in ticketsToCreate)
                    {
                        orderTicketsToCreate.Add(new OrderTicket { OrderId = order.OrderId, TicketId = createdTicket.TicketId });
                    }
                    _context.OrderTickets.AddRange(orderTicketsToCreate);

                    trip.AvailableSeats = (trip.Bus.Capacity ?? 0) - (totalBookedSeatsForThisTrip + bookingRequest.SelectedSeats.Count);
                    trip.UpdatedAt = DateTime.UtcNow;
                    _context.Trips.Update(trip);

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    return new JsonResult(new { success = true, orderId = order.OrderId });
                }
                catch (Exception e)
                {
                    await transaction.RollbackAsync();
                    System.Diagnostics.Debug.WriteLine($"Lỗi khi đặt vé: {e.ToString()}");
                    return new JsonResult(new { success = false, message = "Đã xảy ra lỗi máy chủ trong quá trình đặt vé. Vui lòng thử lại." }) { StatusCode = 500 };
                }
            }
        }
    }
}