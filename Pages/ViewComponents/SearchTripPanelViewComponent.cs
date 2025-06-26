using BusTicketSystem.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace BusTicketSystem.Pages.ViewComponents
{
    public class SearchTripPanelViewModel
    {
        public List<string> DepartureLocations { get; set; } = new List<string>();
        public List<string> DestinationLocations { get; set; } = new List<string>();
        public string? Departure { get; set; }
        public string? Destination{ get; set; }
        public DateTime? DepartureDate { get; set; }
        public DateTime? ReturnDate { get; set; }
        public int Quantity { get; set; } = 1;
        public string? TripType{ get; set; }
        public bool IsRoundTrip { get; set; } = false;
    }

    public class SearchTripPanelViewComponent : ViewComponent
    {
        private readonly AppDbContext _context;
        public SearchTripPanelViewComponent(AppDbContext context)
        {
            _context = context;
        }
        public async Task<IViewComponentResult> InvokeAsync(
            string? departure,
            string? destination,
            DateTime? departureDate,
            DateTime? returnDate,
            int? quantity,
            string? tripType
        )
        {
            var departureLocations = await _context.Routes
                .Where(r => r.Status == Models.RouteStatus.Approved)
                .Select(r => r.Departure)
                .Distinct()
                .OrderBy(d => d)
                .ToListAsync();
            var destinationLocations = await _context.Routes
                .Where(r => r.Status == Models.RouteStatus.Approved)
                .Select(r => r.Destination)
                .Distinct()
                .OrderBy(d => d)
                .ToListAsync();
            var model = new SearchTripPanelViewModel
            {
                DepartureLocations = departureLocations,
                DestinationLocations = destinationLocations,
                Departure = departure,
                Destination = destination,
                DepartureDate = departureDate,
                ReturnDate = returnDate,
                Quantity = quantity ?? 1,
                TripType=tripType??"one-way"
            };
            return View(model);
        }
    }
}