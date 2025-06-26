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
        await Task.CompletedTask;
        PopularTrips = await _context.Trips.Include(t => t.Route).OrderBy(t => t.DepartureTime).Take(6).ToListAsync();
        RecentPosts = await _context.Posts.Where(p => p.Status == PostStatus.Published).OrderByDescending(p => p.CreatedAt).Take(3).ToListAsync();
    }
    public IActionResult OnPostSearch(string departure, string destination, string date, int quantity, string tripType, string? returnDateStr)
    {
        var routeValues = new RouteValueDictionary
        {
            { "departure", departure },
            { "destination", destination },
            { "departureDate", date }, // Giả sử trang Schedule mong đợi 'departureDate'
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