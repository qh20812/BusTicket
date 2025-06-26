using BusTicketSystem.Data;
using BusTicketSystem.Models;
using BusTicketSystem.Models.ViewModels; // Assuming TripViewModel is here
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http; // Required for IHttpContextAccessor
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BusTicketSystem.Pages.ForPartner.TripManage
{
    [Authorize(Roles = "partner")] // Ensure only partners can access
    public class IndexModel : PartnerBasePageModel
    {
        public IndexModel(AppDbContext context, IHttpContextAccessor httpContextAccessor) : base(context, httpContextAccessor)
        {
        }

        public IList<TripViewModel> Trips { get; set; } = new List<TripViewModel>();

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public TripStatus? FilterStatus { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? SortOrder { get; set; }

        // Define a shorter cancellation buffer for partners if needed, or use a system-wide one
        private readonly TimeSpan _cancellationBuffer = TimeSpan.FromHours(2);

        public async Task<IActionResult> OnGetAsync()
        {
            if (PartnerCompanyId == null)
            {
                // Handle case where partner company ID is not found (e.g., not logged in correctly)
                TempData["ErrorMessage"] = "Không thể xác định thông tin nhà xe của bạn.";
                return RedirectToPage("/Index"); // Or an error page
            }

            IQueryable<Trip> query = _context.Trips
                .Include(t => t.Route)
                .Include(t => t.Bus)
                .Include(t => t.Driver)
                .Where(t => t.CompanyId == PartnerCompanyId); // Filter by partner's company

            if (!string.IsNullOrEmpty(SearchTerm))
            {
                query = query.Where(t => (t.Route != null && (t.Route.Departure.Contains(SearchTerm) || t.Route.Destination.Contains(SearchTerm))) ||
                                         (t.Bus != null && t.Bus.LicensePlate.Contains(SearchTerm)) ||
                                         (t.Driver != null && t.Driver.Fullname != null && t.Driver.Fullname.Contains(SearchTerm)));
            }

            if (FilterStatus.HasValue)
            {
                query = query.Where(t => t.Status == FilterStatus.Value);
            }

            switch (SortOrder?.ToLower())
            {
                case "departure_asc":
                    query = query.OrderBy(t => t.DepartureTime);
                    break;
                case "price_asc":
                    query = query.OrderBy(t => t.Price);
                    break;
                case "price_desc":
                    query = query.OrderByDescending(t => t.Price);
                    break;
                case "status_asc":
                    query = query.OrderBy(t => t.Status);
                    break;
                case "status_desc":
                    query = query.OrderByDescending(t => t.Status);
                    break;
                case "departure_desc":
                default:
                    query = query.OrderByDescending(t => t.DepartureTime);
                    break;
            }

            var tripsData = await query.ToListAsync();
            Trips = tripsData.Select(t => new TripViewModel(t, _cancellationBuffer)).ToList();
            // You might want to adjust TripViewModel or add properties to indicate "Pending Admin Approval" more explicitly if needed.
            // The StatusDisplayName should already reflect "Chờ duyệt" due to the DisplayAttribute on the enum.

            return Page();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            if (PartnerCompanyId == null) return Forbid();

            var trip = await _context.Trips
                                .Include(t => t.Tickets)
                                .FirstOrDefaultAsync(t => t.TripId == id && t.CompanyId == PartnerCompanyId);

            if (trip == null) return NotFound();

            // Partners can only delete trips that are PendingApproval or perhaps Cancelled by them, and have no booked tickets.
            if (trip.Status != TripStatus.PendingApproval && trip.Status != TripStatus.Cancelled)
            {
                TempData["ErrorMessage"] = "Bạn chỉ có thể xóa chuyến đi đang chờ duyệt hoặc đã hủy (và chưa có vé).";
                return RedirectToPage();
            }
            // Add more checks as needed (e.g., no booked tickets)

            _context.Trips.Remove(trip);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Đã xóa chuyến đi ID {id} thành công.";
            return RedirectToPage(new { SearchTerm, FilterStatus, SortOrder });
        }
    }
}