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
    public class BusController : ControllerBase
    {
        private readonly AppDbContext _context;

        public BusController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Bus
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Bus>>> GetBuses()
        {
            return await _context.Buses.ToListAsync();
        }

        // GET: api/Bus/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Bus>> GetBus(int id)
        {
            var bus = await _context.Buses.FindAsync(id);
            if (bus == null) return NotFound();
            return bus;
        }

        // POST: api/Bus
        [HttpPost]
        public async Task<ActionResult<Bus>> CreateBus(Bus bus)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            _context.Buses.Add(bus);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetBus), new { id = bus.BusId }, bus);
        }

        // PUT: api/Bus/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateBus(int id, Bus bus)
        {
            if (id != bus.BusId) return BadRequest();
            _context.Entry(bus).State = EntityState.Modified;
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Buses.Any(e => e.BusId == id)) return NotFound();
                throw;
            }
            return NoContent();
        }

        // DELETE: api/Bus/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBus(int id)
        {
            var bus = await _context.Buses.FindAsync(id);
            if (bus == null) return NotFound();
            _context.Buses.Remove(bus);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}