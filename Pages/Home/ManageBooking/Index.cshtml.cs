using BusTicketSystem.Data;
using BusTicketSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace BusTicketSystem.Pages.Home.ManageBooking
{
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;
        public IndexModel(AppDbContext context)
        {
            _context = context;
        }

        public IList<Ticket> Tickets { get; set; } = new List<Ticket>();
        
        [BindProperty]
        [Display(Name = "Số điện thoại")]
        [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
        [Phone(ErrorMessage = "Định dạng số điện thoại không hợp lệ.")]
        public string PhoneNumber { get; set; } = string.Empty;

        public bool SearchPerformed { get; private set; } = false;

        public void OnGet()
        {
            // Khởi tạo trang, Tickets đã được khởi tạo rỗng ở trên
        }

        public async Task<IActionResult> OnPostAsync()
        {
            SearchPerformed = true;

            if (!ModelState.IsValid)
            {
                Tickets = new List<Ticket>();
                return Page();
            }
            Tickets = await _context.Tickets
                .Include(t => t.User) 
                .Include(t => t.Trip)
                    .ThenInclude(trip => trip.Route)
                .Include(t=>t.Order).Where(t=>(t.User!=null&&t.User.Phone==PhoneNumber)||(t.Order!=null&&t.Order.GuestPhone==PhoneNumber))
                .OrderByDescending(t => t.BookedAt).Distinct()
                .ToListAsync();
            return Page();
        }
    }
}