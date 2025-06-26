using BusTicketSystem.Data;
using BusTicketSystem.Helpers;
using BusTicketSystem.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BusTicketSystem.Pages.ForPartner.Dashboard
{
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public IndexModel(AppDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public string Username { get; set; } = string.Empty;
        public List<Order> Orders { get; set; } = new List<Order>();
        public string StatusVietnamese { get; set; } = string.Empty; // This seems to be a property for a single order, might need adjustment for a list
        public int PaidTicketCount { get; set; }
        public decimal TotalAmount { get; set; }
        public int TotalTicketCount { get; set; }
        public int CancelledTicketCount { get; set; }
        public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(string SearchName, string SortName)
        {
            var userId = _httpContextAccessor.HttpContext?.Session.GetUserId();
            if (!userId.HasValue)
            {
                return RedirectToPage("/Account/Login/Login");
            }

            var currentUser = await _context.Users.FindAsync(userId.Value);
            if (currentUser == null || !currentUser.CompanyId.HasValue)
            {
                ErrorMessage = "Tài khoản của bạn chưa được liên kết với một nhà xe hoặc không tìm thấy thông tin nhà xe.";
                // Initialize counts to 0 to avoid null reference issues in the view
                PaidTicketCount = 0;
                TotalAmount = 0;
                TotalTicketCount = 0;
                CancelledTicketCount = 0;
                Orders = new List<Order>();
                return Page();
            }
            var partnerCompanyId = currentUser.CompanyId.Value;
            Username = currentUser.Username;

            // Calculate statistics for the partner's company
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            PaidTicketCount = await _context.OrderTickets
                .Where(ot => ot.Ticket.Trip.CompanyId == partnerCompanyId && ot.Order.Status == OrderStatus.Paid)
                .CountAsync();
#pragma warning restore CS8602 // Dereference of a possibly null reference.

#pragma warning disable CS8602 // Dereference of a possibly null reference.
            CancelledTicketCount = await _context.Tickets
                .Where(t => t.Trip.CompanyId == partnerCompanyId && t.Status == TicketStatus.Cancelled)
                .CountAsync();
#pragma warning restore CS8602 // Dereference of a possibly null reference.

#pragma warning disable CS8602 // Dereference of a possibly null reference.
            TotalTicketCount = await _context.Tickets
                .Where(t => t.Trip.CompanyId == partnerCompanyId)
                .CountAsync();
#pragma warning restore CS8602 // Dereference of a possibly null reference.

#pragma warning disable CS8602 // Dereference of a possibly null reference.
            TotalAmount = await _context.Tickets
                .Where(t => t.Trip.CompanyId == partnerCompanyId && t.Order.Status == OrderStatus.Paid)
                .SumAsync(t => (decimal?)t.Price) ?? 0;
#pragma warning restore CS8602 // Dereference of a possibly null reference.

            // Get recent orders for the partner's company
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            IQueryable<Order> query = _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderTickets)
                    .ThenInclude(ot => ot.Ticket)
                        .ThenInclude(t => t!.Trip) // Assuming Ticket will always have a Trip
                .Where(o => o.OrderTickets.Any(ot => ot.Ticket.Trip.CompanyId == partnerCompanyId));
#pragma warning restore CS8602 // Dereference of a possibly null reference.

            if (!string.IsNullOrEmpty(SearchName))
            {
                query = query.Where(o => (o.User != null && o.User.Fullname != null && o.User.Fullname.Contains(SearchName)) || (o.GuestEmail != null && o.GuestEmail.Contains(SearchName)));
            }

            // Sorting logic (example: by date, can be expanded)
            query = query.OrderByDescending(o => o.CreatedAt);

            Orders = await query.Take(10).ToListAsync(); // Take 10 recent orders

            return Page();
        }
        private string ConvertStatusToVietnamese(string status)
        {
            return status.ToLower() switch
            {
                "paid" => "Đã thanh toán",
                "pending" => "Đang chờ",
                "cancelled" => "Đã hủy",
                "refunded" => "Đã hoàn tiền",
                _ => status,
            };
        }
    }
}