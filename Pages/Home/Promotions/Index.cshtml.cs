using BusTicketSystem.Data;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BusTicketSystem.Pages.Home.Promotions
{
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;
        public IndexModel(AppDbContext context)
        {
            _context = context;
        }


    }
}