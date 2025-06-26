using BusTicketSystem.Data;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BusTicketSystem.Pages.Home.AboutUs
{
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;
        public IndexModel(AppDbContext context)
        {
            _context = context;
        }
        public void OnGet()
        {
            
        }
    }
}