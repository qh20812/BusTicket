using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BusTicketSystem.Models;
using BusTicketSystem.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BusTicketSystem.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderController : ControllerBase
    {
        private readonly AppDbContext _context;

        public OrderController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Order
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Order>>> GetOrders()
        {
            return await _context.Orders
                .Include(o => o.User)
                .Include(o => o.Promotion)
                .Include(o => o.Payments)
                .ToListAsync();
        }

        // GET: api/Order/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Order>> GetOrder(int id)
        {
            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.Promotion)
                .Include(o => o.Payments)
                .FirstOrDefaultAsync(o => o.OrderId == id);
            if (order == null) return NotFound();
            return order;
        }

        // POST: api/Order
        [HttpPost]
        public async Task<ActionResult<Order>> CreateOrder(Order order)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            // Lấy userId từ Claims (Identity)
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized("User is not logged in.");

            // Nếu Order.UserId là int, cần ép kiểu
            if (int.TryParse(userId, out int parsedUserId))
            {
                order.UserId = parsedUserId;
            }
            else
            {
                return BadRequest("Invalid user ID.");
            }

            // XỬ LÝ GÁN LOẠI VÉ (TicketType) CHO TỪNG VÉ TRONG ĐƠN HÀNG
            // Giả sử client truyền TripType ("one-way" hoặc "round-trip") và ngày về vào order (nếu có)
            // Nếu không có property TripType trong Order, bạn cần bổ sung hoặc lấy từ request
            string tripType = null;
            if (Request.HasFormContentType && Request.Form.ContainsKey("TripType"))
            {
                tripType = Request.Form["TripType"].ToString();
            }
            // Nếu Order có property TripType thì dùng: tripType = order.TripType;

            if (order.Tickets != null && order.Tickets.Any())
            {
                foreach (var ticket in order.Tickets)
                {
                    if (!string.IsNullOrEmpty(tripType) && tripType == "round-trip")
                    {
                        ticket.Type = TicketType.RoundTrip;
                    }
                    else
                    {
                        ticket.Type = TicketType.OneWay;
                    }
                }
            }

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetOrder), new { id = order.OrderId }, order);
        }

        // PUT: api/Order/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateOrder(int id, Order order)
        {
            if (id != order.OrderId) return BadRequest();
            _context.Entry(order).State = EntityState.Modified;
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Orders.Any(e => e.OrderId == id)) return NotFound();
                throw;
            }
            return NoContent();
        }

        // DELETE: api/Order/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOrder(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound();
            _context.Orders.Remove(order);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}