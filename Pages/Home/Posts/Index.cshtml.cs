using BusTicketSystem.Data;
using BusTicketSystem.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BusTicketSystem.Pages.Home.Posts
{
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;

        public IndexModel(AppDbContext context)
        {
            _context = context;
        }

        public IList<Post> Posts { get;set; } = new List<Post>();

        public async Task OnGetAsync()
        {
            Posts = await _context.Posts
                .Where(p => p.Status == PostStatus.Published) // Chỉ lấy bài đã xuất bản
                .Include(p => p.User) // Lấy thông tin người đăng nếu cần hiển thị
                .OrderByDescending(p => p.CreatedAt) // Sắp xếp bài mới nhất lên đầu
                .ToListAsync();
        }
    }
}