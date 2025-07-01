using BusTicketSystem.Data;
using BusTicketSystem.Models;
using BusTicketSystem.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Routing;

namespace BusTicketSystem.Pages;

public class IndexModel(AppDbContext context) : PageModel
{
    private readonly AppDbContext _context = context;
    
    public List<TripViewModel> Popular { get; set; } = new List<TripViewModel>();
    public List<Trip> PopularTrips { get; set; } = new List<Trip>();
    public List<Post> RecentPosts { get; set; } = new List<Post>();
    public List<PromotionViewModel> PopularPromotions { get; set; } = new List<PromotionViewModel>();

    public async Task OnGetAsync()
    {
        PopularTrips = await _context.Trips
            .Include(t => t.Route)
            .Where(t => t.Status == TripStatus.Scheduled)
            .OrderBy(t => t.DepartureTime)
            .Take(8)
            .ToListAsync();

        var popularTrip = await _context.Routes
            .Where(r => r.Status == RouteStatus.Approved)
            .Take(8)
            .ToListAsync();

        Popular = popularTrip.Select(r => new TripViewModel(new Trip { Route = r, DepartureTime = DateTime.UtcNow }, TimeSpan.Zero)).ToList();

        RecentPosts = await _context.Posts
            .Where(p => p.Status == PostStatus.Published)
            .OrderByDescending(p => p.CreatedAt)
            .Take(4)
            .ToListAsync();
    }

    public IActionResult OnPostSearch(string departure, string destination, string date, int quantity, string tripType, string? returnDateStr)
    {
        var routeValues = new RouteValueDictionary
        {
            { "departure", departure },
            { "destination", destination },
            { "departureDate", date },
            { "quantity", quantity },
            { "tripType", tripType }
        };

        if (tripType == "round-trip" && !string.IsNullOrEmpty(returnDateStr))
        {
            routeValues.Add("returnDate", returnDateStr);
        }
        return RedirectToPage("/Home/Schedule/Index", routeValues);
    }
}