using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BusTicketSystem.Data;
using BusTicketSystem.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BusTicketSystem.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;

        public HomeController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Load routes for search form
            var routes = await _context.Routes.ToListAsync();
            ViewBag.Routes = routes;

            // Load 5 most popular trips
            var popularTrips = await _context.Trips
                .Include(t => t.Route)
                .Include(t => t.Bus)
                .Include(t => t.Company)
                .GroupJoin(_context.Tickets.Where(t => t.Status == TicketStatus.Booked || t.Status == TicketStatus.Used),
                    trip => trip.TripId,
                    ticket => ticket.TripId,
                    (trip, tickets) => new
                    {
                        Trip = trip,
                        TicketCount = tickets.Count()
                    })
                .OrderByDescending(t => t.TicketCount)
                .Take(5)
                .Select(t => t.Trip)
                .ToListAsync();

            ViewBag.PopularTrips = popularTrips;

            return View();
        }

        [HttpPost]
        public IActionResult Search(string departure, string destination, string date, int tickets)
        {
            // Redirect to Trip controller or handle search logic
            return RedirectToAction("Index", "Trip", new { departure, destination, date, tickets });
        }
    }
}