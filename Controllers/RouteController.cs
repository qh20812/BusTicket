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
    public class RouteController : ControllerBase
    {
        private readonly AppDbContext _context;

        public RouteController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Route
        [HttpGet]
        public async Task<ActionResult<IEnumerable<BusTicketSystem.Models.Route>>> GetRoutes()
        {
            return await _context.Routes.ToListAsync();
        }

        // GET: api/Route/5
        [HttpGet("{id}")]
        public async Task<ActionResult<BusTicketSystem.Models.Route>> GetRoute(int id)
        {
            var route = await _context.Routes.FindAsync(id);
            if (route == null) return NotFound();
            return route;
        }

        // POST: api/Route
        [HttpPost]
        public async Task<ActionResult<BusTicketSystem.Models.Route>> CreateRoute(BusTicketSystem.Models.Route route)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            _context.Routes.Add(route);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetRoute), new { id = route.RouteId }, route);
        }

        // PUT: api/Route/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateRoute(int id, BusTicketSystem.Models.Route route)
        {
            if (id != route.RouteId) return BadRequest();
            _context.Entry(route).State = EntityState.Modified;
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Routes.Any(e => e.RouteId == id)) return NotFound();
                throw;
            }
            return NoContent();
        }

        // DELETE: api/Route/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRoute(int id)
        {
            var route = await _context.Routes.FindAsync(id);
            if (route == null) return NotFound();
            _context.Routes.Remove(route);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}