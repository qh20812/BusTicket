using BusTicketSystem.Data;
using BusTicketSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace BusTicketSystem.Pages.ForAdmin.OrderManage
{
    public class TicketInfoModel : PageModel
    {
        private readonly AppDbContext _context;

        public TicketInfoModel(AppDbContext context)
        {
            _context = context;
        }

        public Order? Order { get; set; }

        public async Task<IActionResult> OnGetAsync(int? orderId)
        {
            if (orderId == null)
            {
                return NotFound("Yêu cầu mã hóa đơn.");
            }

            Order = await _context.Orders
                .Include(o => o.OrderTickets)
                    .ThenInclude(ot => ot.Ticket)
                        .ThenInclude(t => t!.Trip) 
                            .ThenInclude(tr => tr!.Route) 
                .Include(o => o.User)
                .Include(o => o.Promotion)
                .FirstOrDefaultAsync(m => m.OrderId == orderId);

            if (Order == null)
            {
                return NotFound($"Không tìm thấy đơn hàng với ID {orderId}.");
            }

            // Logic chuyển đổi trạng thái đã có trong OrderViewModel, nhưng nếu cần trực tiếp:
            
            return Page();
        }
    }
}