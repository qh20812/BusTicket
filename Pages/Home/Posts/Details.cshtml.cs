using BusTicketSystem.Data;
using BusTicketSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace BusTicketSystem.Pages.Home.Posts
{
    public class DetailsModel : PageModel
    {
        private readonly AppDbContext _context;

        public DetailsModel(AppDbContext context)
        {
            _context = context;
        }

        public Post? Post { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Post = await _context.Posts
                .Include(p => p.User) // Nạp thông tin người đăng
                .FirstOrDefaultAsync(p => p.PostId == id && p.Status == PostStatus.Published); // Chỉ lấy bài đã xuất bản

            if (Post == null)
            {
                return NotFound(); // Không tìm thấy bài viết hoặc bài viết chưa được xuất bản
            }
            ViewData["BodyClass"] = "post-detail-page";
            return Page();
        }
    }
}