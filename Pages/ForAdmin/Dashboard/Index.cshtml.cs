using BusTicketSystem.Data;
using BusTicketSystem.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.DataAnnotations.Schema;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace BusTicketSystem.Pages.ForAdmin.Dashboard
{
    [Authorize(Roles ="Admin")]
    public class IndexModel(AppDbContext context) : PageModel
    {
        
        private readonly AppDbContext _context = context;

        public required string Username { get; set; }
        public DateTime OrderDate { get; set; }
        public required List<Order> Orders { get; set; }
        public required List<User> Users { get; set; }
        public required string StatusVietnamese { get; set; }
        public int PaidTicketCount { get; set; }
        public decimal TotalAmount { get; set; }
        public int TotalTicketCount { get; set; }
        public int CancelledTicketCount { get; set; }
        [BindProperty(SupportsGet = true)]
        public string? SearchName { get; set; }
        [BindProperty(SupportsGet = true)]
        public string? SortName{ get; set; }
        public async Task<IActionResult> OnGetAsync(string SearchName, string SortName)
        {
            // Reverted to original data fetching logic
            Orders = await _context.Orders.Include(o => o.User).Include(o => o.OrderTickets).ToListAsync();
            Users = await _context.Users.Where(u => u.Role == "member" && u.Role != null).ToListAsync();
            if (Orders.Any())
            {
#pragma warning disable CS8601 // Possible null reference assignment.
                Username = Orders.First().User?.Username;
#pragma warning restore CS8601 // Possible null reference assignment.
                OrderDate = Orders.First().CreatedAt;
            }
            PaidTicketCount = Orders.Where(o => o.Status == OrderStatus.Paid).SelectMany(o => o.OrderTickets ?? Enumerable.Empty<OrderTicket>()).Count();
            TotalTicketCount = await _context.OrderTickets.CountAsync();
            TotalAmount = Orders.Where(o => o.Status == OrderStatus.Paid).Sum(o => o.TotalAmount);
            CancelledTicketCount = await _context.Cancellations.CountAsync();
            var query = _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderTickets).AsQueryable();

#pragma warning disable CS8602 // Dereference of a possibly null reference.
            if (!string.IsNullOrEmpty(SearchName))
            {
                query = query.Where(o => o.User.Fullname.Contains(SearchName));
            }

            switch (SortName?.ToLower())
            {
                case "az":
                    query = query.OrderBy(o => o.User.Fullname); break;
                case "za":
                    query = query.OrderByDescending(o => o.User.Fullname); break;
                case "oldest":
                    query = query.OrderBy(o => o.CreatedAt); break;
                default:
                    query = query.OrderByDescending(o => o.CreatedAt); break;
            }

            Orders = await query.ToListAsync();

            return Page();
        }
        private string ConvertStatusToVietnamese(string status)
        {
            switch (status.ToLower())
            {
                case "paid":
                    return "Đã thanh toán";
                case "pending":
                    return "Đang chờ";
                case "cancelled":
                    return "Đã hủy";
                case "refunded":
                    return "Đã hoàn tiền";
                default:
                    return status;
            }
        }
    }
}