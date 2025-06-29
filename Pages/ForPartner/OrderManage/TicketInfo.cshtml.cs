using BusTicketSystem.Data;
using BusTicketSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace BusTicketSystem.Pages.ForPartner.OrderManage
{
    [Authorize(Roles = "Partner")]
    public class TicketInfoModel : PageModel
    {
        private readonly AppDbContext _context;

        public TicketInfoModel(AppDbContext context)
        {
            _context = context;
        }

        public Order? Order { get; set; }
        public List<Ticket> PartnerTickets { get; set; } = new List<Ticket>();
        public string OrderStatusDisplay { get; set; } = string.Empty;

        private int GetCurrentPartnerBusCompanyId()
        {
            // IMPORTANT: Implement this method to securely get the logged-in partner's BusCompanyId
            var busCompanyIdClaim = User.FindFirstValue("CompanyId"); // Changed to "CompanyId"
            if (int.TryParse(busCompanyIdClaim, out int busCompanyId))
            {
                return busCompanyId;
            }
            throw new System.Exception("BusCompanyId not found for the current partner.");
        }

        public async Task<IActionResult> OnGetAsync(int orderId)
        {
            int partnerBusCompanyId = GetCurrentPartnerBusCompanyId();

            Order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.Promotion)
                .Include(o => o.OrderTickets)
                    .ThenInclude(ot => ot.Ticket)
                        .ThenInclude(t => t!.Trip)
                            .ThenInclude(tr => tr!.Route) // Include Route for display purposes
                .FirstOrDefaultAsync(m => m.OrderId == orderId &&
                                          m.OrderTickets.Any(ot => ot.Ticket != null && ot.Ticket.Trip != null && ot.Ticket.Trip.CompanyId == partnerBusCompanyId));

            if (Order == null)
            {
                // Order not found or doesn't belong to this partner
                return NotFound($"Không tìm thấy đơn hàng với ID {orderId} hoặc đơn hàng không thuộc nhà xe của bạn.");
            }

            // Filter tickets to show only those belonging to the current partner
            PartnerTickets = Order.OrderTickets
                                .Where(ot => ot.Ticket != null && ot.Ticket.Trip != null && ot.Ticket.Trip.CompanyId == partnerBusCompanyId)
                                .Select(ot => ot.Ticket!)
                                .ToList();

            if (!PartnerTickets.Any())
            {
                // This case should ideally be caught by the initial query, but as a safeguard:
                return NotFound($"Đơn hàng ID {orderId} không có vé nào thuộc nhà xe của bạn.");
            }

            // Use the helper from PartnerOrderViewModel for consistency
            OrderStatusDisplay = BusTicketSystem.Pages.ForPartner.OrderManage.PartnerOrderViewModel.GetStatusDisplayName(Order.Status);
            return Page();
        }
    }
}