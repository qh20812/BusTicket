using BusTicketSystem.Data;
using BusTicketSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace BusTicketSystem.Pages.ForCustomer.TicketInvoice
{
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;

        public IndexModel(AppDbContext context)
        {
            _context = context;
        }

        public Order? Order { get; set; }
        [TempData]
        public string? InfoMessage { get; set; } // Để hiển thị thông báo

        public async Task<IActionResult> OnGetAsync(int? orderId)
        {
            if (orderId == null)
            {
                // Nếu không có orderId, có thể chuyển hướng hoặc hiển thị lỗi
                // Ví dụ: return RedirectToPage("/Index");
                TempData["ErrorMessage"] = "Yêu cầu mã hóa đơn.";
                return RedirectToPage("/Index"); // Hoặc trang lỗi chung
            }

            Order = await _context.Orders
                .Include(o => o.OrderTickets)
                    .ThenInclude(ot => ot.Ticket)
                        .ThenInclude(t => t!.Trip) // Sử dụng ! để báo cho compiler là Trip không null ở đây (nếu logic đảm bảo)
                            .ThenInclude(tr => tr!.Route) // Tương tự cho Route
                .Include(o => o.User)
                .Include(o => o.Promotion).Include(o=>o.Payments)
                .FirstOrDefaultAsync(m => m.OrderId == orderId);

            if (Order == null)
            {
                TempData["ErrorMessage"] = $"Không tìm thấy hóa đơn với ID {orderId}.";
                return RedirectToPage("/Index"); // Hoặc trang lỗi chung
            }

            // Kiểm tra và tự động hủy đơn hàng nếu quá hạn 1 tiếng và đang chờ thanh toán
            if (Order.Status == OrderStatus.Pending && Order.CreatedAt.AddHours(1) < DateTime.UtcNow)
            {
                var ticketsToCancel = await _context.Tickets
                    .Where(t => t.OrderId == Order.OrderId && t.Status == TicketStatus.Booked)
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

                Order.Status = OrderStatus.Cancelled;
                Order.UpdatedAt = DateTime.UtcNow;
                _context.Orders.Update(Order);

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

                InfoMessage = "Đơn hàng này đã tự động bị hủy do quá hạn thanh toán (sau 1 giờ).";
                // Tải lại Order để hiển thị trạng thái mới nhất
                Order = await _context.Orders
                    .Include(o => o.OrderTickets).ThenInclude(ot => ot.Ticket).ThenInclude(t => t!.Trip).ThenInclude(tr => tr!.Route)
                    .Include(o => o.User).Include(o => o.Promotion).Include(o => o.Payments)
                    .FirstOrDefaultAsync(m => m.OrderId == orderId);
            }

            return Page();
        }
    }
}