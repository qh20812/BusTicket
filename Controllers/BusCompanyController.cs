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
    public class BusCompanyController : ControllerBase
    {
        private readonly AppDbContext _context;

        public BusCompanyController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/BusCompany
        [HttpGet]
        public async Task<ActionResult<IEnumerable<BusCompany>>> GetBusCompanies()
        {
            return await _context.BusCompanies.ToListAsync();
        }

        // GET: api/BusCompany/5
        [HttpGet("{id}")]
        public async Task<ActionResult<BusCompany>> GetBusCompany(int id)
        {
            var busCompany = await _context.BusCompanies.FindAsync(id);
            if (busCompany == null) return NotFound();
            return busCompany;
        }

        // POST: api/BusCompany
        [HttpPost]
        public async Task<ActionResult<BusCompany>> CreateBusCompany(BusCompany busCompany)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            _context.BusCompanies.Add(busCompany);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetBusCompany), new { id = busCompany.CompanyId }, busCompany);
        }

        // PUT: api/BusCompany/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateBusCompany(int id, BusCompany busCompany)
        {
            if (id != busCompany.CompanyId) return BadRequest();
            _context.Entry(busCompany).State = EntityState.Modified;
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.BusCompanies.Any(e => e.CompanyId == id)) return NotFound();
                throw;
            }
            return NoContent();
        }

        // DELETE: api/BusCompany/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBusCompany(int id)
        {
            var busCompany = await _context.BusCompanies.FindAsync(id);
            if (busCompany == null) return NotFound();
            _context.BusCompanies.Remove(busCompany);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}