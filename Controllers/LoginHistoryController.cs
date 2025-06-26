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
    public class LoginHistoryController : ControllerBase
    {
        private readonly AppDbContext _context;

        public LoginHistoryController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/LoginHistory
        [HttpGet]
        public async Task<ActionResult<IEnumerable<LoginHistory>>> GetLoginHistories()
        {
            return await _context.LoginHistory
                .Include(lh => lh.User)
                .ToListAsync();
        }

        // GET: api/LoginHistory/5
        [HttpGet("{id}")]
        public async Task<ActionResult<LoginHistory>> GetLoginHistory(int id)
        {
            var loginHistory = await _context.LoginHistory
                .Include(lh => lh.User)
                .FirstOrDefaultAsync(lh => lh.Id == id);
            if (loginHistory == null) return NotFound();
            return loginHistory;
        }

        // POST: api/LoginHistory
        [HttpPost]
        public async Task<ActionResult<LoginHistory>> CreateLoginHistory(LoginHistory loginHistory)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            _context.LoginHistory.Add(loginHistory);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetLoginHistory), new { id = loginHistory.Id }, loginHistory);
        }

        // PUT: api/LoginHistory/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateLoginHistory(int id, LoginHistory loginHistory)
        {
            if (id != loginHistory.Id) return BadRequest();
            _context.Entry(loginHistory).State = EntityState.Modified;
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.LoginHistory.Any(e => e.Id == id)) return NotFound();
                throw;
            }
            return NoContent();
        }

        // DELETE: api/LoginHistory/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteLoginHistory(int id)
        {
            var loginHistory = await _context.LoginHistory.FindAsync(id);
            if (loginHistory == null) return NotFound();
            _context.LoginHistory.Remove(loginHistory);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}