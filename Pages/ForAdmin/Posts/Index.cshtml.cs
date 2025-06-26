using BusTicketSystem.Data;
using BusTicketSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;

namespace BusTicketSystem.Pages.ForAdmin.Posts
{
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;
        public IndexModel(AppDbContext context)
        {
            _context = context;
        }
        // Danh sách Post sẽ trả về cho View
        public IList<Post> Posts { get; set; } = new List<Post>();

        [BindProperty]
        public Post Post { get; set; } = new Post();

        // Async method để load dữ liệu Post cùng User liên quan
        public async Task OnGetAsync()
        {
            Posts = await _context.Posts
                .Include(p => p.User) // Nạp thêm thông tin người đăng bài
                .OrderByDescending(p => p.CreatedAt) // Sắp xếp bài viết mới nhất lên đầu
                .ToListAsync();
        }


        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            var userId = HttpContext.Session.GetInt32("UserId");

#pragma warning disable CS8629 // Nullable value type may be null.
            Post.UserId = userId.Value;
#pragma warning restore CS8629 // Nullable value type may be null.
            Post.CreatedAt = DateTime.UtcNow;
            Post.UpdatedAt = DateTime.UtcNow;

            _context.Posts.Add(Post);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
        
        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var postToDelete = await _context.Posts.FindAsync(id);

            if (postToDelete == null)
            {
                return NotFound();
            }

            _context.Posts.Remove(postToDelete);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã xóa bài đăng \"{postToDelete.Title}\" thành công.";
            return RedirectToPage("./Index");
        }
    }
}