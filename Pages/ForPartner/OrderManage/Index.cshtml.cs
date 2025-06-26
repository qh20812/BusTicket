using BusTicketSystem.Data;
using BusTicketSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace BusTicketSystem.Pages.ForPartner.OrderManage
{
    public class PartnerOrderViewModel
    {
        public int OrderId { get; set; }
        public string? CustomerName { get; set; }
        public string? CustomerEmail { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; } // Total amount of the whole order
        public string TotalAmountDisplay => TotalAmount.ToString("N0") + " VND";
        public OrderStatus Status { get; set; }
        public string StatusDisplayName { get; set; }
        public string StatusCssClass { get; set; }
        public int TicketCountForPartner { get; set; } // Number of tickets in this order relevant to the partner
        public string? FirstTripInfoForPartner { get; set; } // Info of the first trip relevant to the partner

        public PartnerOrderViewModel(Order order, int partnerBusCompanyId)
        {
            OrderId = order.OrderId;
            CustomerName = order.User?.Fullname ?? order.GuestEmail ?? "Khách vãng lai";
            CustomerEmail = order.User?.Email ?? order.GuestEmail;
            OrderDate = order.CreatedAt;
            TotalAmount = order.TotalAmount;
            Status = order.Status;
            StatusDisplayName = GetStatusDisplayName(order.Status);
            StatusCssClass = GetStatusCssClass(order.Status);

            var partnerTickets = order.OrderTickets
                .Where(ot => ot.Ticket != null && ot.Ticket.Trip != null && ot.Ticket.Trip.CompanyId == partnerBusCompanyId)
                .Select(ot => ot.Ticket)
                .ToList();

            TicketCountForPartner = partnerTickets.Count;

            if (partnerTickets.Any())
            {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                var firstPartnerTicket = partnerTickets.OrderBy(t => t.Trip?.DepartureTime).FirstOrDefault();
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                if (firstPartnerTicket?.Trip?.Route != null)
                {
                    FirstTripInfoForPartner = $"{firstPartnerTicket.Trip.Route.Departure} \u2192 {firstPartnerTicket.Trip.Route.Destination} ({firstPartnerTicket.Trip.DepartureTime:dd/MM/yyyy HH:mm})";
                }
                else
                {
                    FirstTripInfoForPartner = "N/A";
                }
            }
            else
            {
                FirstTripInfoForPartner = "N/A";
            }
        }

        public static string GetStatusDisplayName(OrderStatus status)
        {
            // This can be shared from a common utility or duplicated if preferred
            var field = typeof(OrderStatus).GetField(status.ToString());
            var displayAttribute = field?.GetCustomAttributes(typeof(System.ComponentModel.DataAnnotations.DisplayAttribute), false).FirstOrDefault() as System.ComponentModel.DataAnnotations.DisplayAttribute;
            if (displayAttribute != null) return displayAttribute.GetName() ?? status.ToString();
            return status switch
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

    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;

        public IndexModel(AppDbContext context)
        {
            _context = context;
        }

        public IList<PartnerOrderViewModel> OrdersDisplay { get; set; } = new List<PartnerOrderViewModel>();

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public OrderStatus? FilterStatus { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? SortOrder { get; set; }

        public SelectList Statuses { get; set; } = new SelectList(new List<SelectListItem>());

        private int GetCurrentPartnerBusCompanyId()
        {
            // IMPORTANT: Implement this method to securely get the logged-in partner's BusCompanyId
            // Example: using claims
            var busCompanyIdClaim = User.FindFirstValue("CompanyId"); // Changed to "CompanyId"
            if (int.TryParse(busCompanyIdClaim, out int busCompanyId))
            {
                return busCompanyId;
            }
            // Fallback or error handling if BusCompanyId is not found
            // Forcing a redirect or showing an error might be appropriate here
            throw new System.Exception("BusCompanyId not found for the current partner.");
        }

        public async Task<IActionResult> OnGetAsync()
        {
            int partnerBusCompanyId = GetCurrentPartnerBusCompanyId();

            IQueryable<Order> query = _context.Orders
                .Include(o => o.User)
                .Include(o => o.Promotion)
                .Include(o => o.OrderTickets)
                    .ThenInclude(ot => ot.Ticket)
                        .ThenInclude(t => t!.Trip)
                            .ThenInclude(tr => tr!.Route) // Route is needed for FirstTripInfoForPartner
                .Where(o => o.OrderTickets.Any(ot => ot.Ticket != null && ot.Ticket.Trip != null && ot.Ticket.Trip.CompanyId == partnerBusCompanyId)); // Key filter for partner

            if (!string.IsNullOrEmpty(SearchTerm))
            {
                string searchTermLower = SearchTerm.ToLower();
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                query = query.Where(o =>
                    o.OrderId.ToString().Contains(searchTermLower) ||
                    (o.User != null && (o.User.Fullname.Contains(searchTermLower, StringComparison.CurrentCultureIgnoreCase) || o.User.Email.ToLower().Contains(searchTermLower))) ||
                    (o.GuestEmail != null && o.GuestEmail.ToLower().Contains(searchTermLower)) ||
                    (o.OrderTickets.Any(ot => ot.TicketId.ToString().Contains(searchTermLower) && ot.Ticket!.Trip!.CompanyId == partnerBusCompanyId)) // Ensure search within partner's tickets
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
                default: // "date_desc" or default
                    query = query.OrderByDescending(o => o.CreatedAt);
                    break;
            }

            var orders = await query.ToListAsync();
            OrdersDisplay = orders.Select(o => new PartnerOrderViewModel(o, partnerBusCompanyId)).ToList();

            var enumValues = System.Enum.GetValues(typeof(OrderStatus)).Cast<OrderStatus>();
            Statuses = new SelectList(enumValues.Select(e => new SelectListItem
            {
                Value = e.ToString(),
                Text = PartnerOrderViewModel.GetStatusDisplayName(e)
            }), "Value", "Text", FilterStatus?.ToString());

            return Page();
        }
    }
}