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
    public class OrderTicketController : ControllerBase
    {
        private readonly AppDbContext _context;

        public OrderTicketController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/OrderTicket
        [HttpGet]
        public async Task<ActionResult<IEnumerable<OrderTicket>>> GetOrderTickets()
        {
            return await _context.OrderTickets
                .Include(ot => ot.Order)
                .Include(ot => ot.Ticket)
                .ToListAsync();
        }

        // GET: api/OrderTicket/5
        [HttpGet("{id}")]
        public async Task<ActionResult<OrderTicket>> GetOrderTicket(int id)
        {
            var orderTicket = await _context.OrderTickets
                .Include(ot => ot.Order)
                .Include(ot => ot.Ticket)
                .FirstOrDefaultAsync(ot => ot.OrderTicketId == id);
            if (orderTicket == null) return NotFound();
            return orderTicket;
        }

        // POST: api/OrderTicket
        [HttpPost]
        public async Task<ActionResult<OrderTicket>> CreateOrderTicket(OrderTicket orderTicket)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            _context.OrderTickets.Add(orderTicket);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetOrderTicket), new { id = orderTicket.OrderTicketId }, orderTicket);
        }

        // PUT: api/OrderTicket/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateOrderTicket(int id, OrderTicket orderTicket)
        {
            if (id != orderTicket.OrderTicketId) return BadRequest();
            _context.Entry(orderTicket).State = EntityState.Modified;
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.OrderTickets.Any(e => e.OrderTicketId == id)) return NotFound();
                throw;
            }
            return NoContent();
        }

        // DELETE: api/OrderTicket/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOrderTicket(int id)
        {
            var orderTicket = await _context.OrderTickets.FindAsync(id);
            if (orderTicket == null) return NotFound();
            _context.OrderTickets.Remove(orderTicket);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}