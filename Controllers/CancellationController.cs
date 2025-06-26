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
    public class CancellationController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CancellationController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Cancellation
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Cancellation>>> GetCancellations()
        {
            return await _context.Cancellations
                .Include(c => c.Ticket)
                .Include(c => c.Order)
                .ToListAsync();
        }

        // GET: api/Cancellation/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Cancellation>> GetCancellation(int id)
        {
            var cancellation = await _context.Cancellations
                .Include(c => c.Ticket)
                .Include(c => c.Order)
                .FirstOrDefaultAsync(c => c.CancellationId == id);
            if (cancellation == null) return NotFound();
            return cancellation;
        }

        // POST: api/Cancellation
        [HttpPost]
        public async Task<ActionResult<Cancellation>> CreateCancellation(Cancellation cancellation)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            _context.Cancellations.Add(cancellation);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetCancellation), new { id = cancellation.CancellationId }, cancellation);
        }

        // PUT: api/Cancellation/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCancellation(int id, Cancellation cancellation)
        {
            if (id != cancellation.CancellationId) return BadRequest();
            _context.Entry(cancellation).State = EntityState.Modified;
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Cancellations.Any(e => e.CancellationId == id)) return NotFound();
                throw;
            }
            return NoContent();
        }

        // DELETE: api/Cancellation/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCancellation(int id)
        {
            var cancellation = await _context.Cancellations.FindAsync(id);
            if (cancellation == null) return NotFound();
            _context.Cancellations.Remove(cancellation);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}