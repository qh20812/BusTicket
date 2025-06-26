using BusTicketSystem.Data;
using BusTicketSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BusTicketSystem.Pages.ForAdmin.Posts
{
    public class EditModel(AppDbContext context) : PageModel
    {
        private readonly AppDbContext _context = context;

        [BindProperty]
        public required Post Post { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
#pragma warning disable CS8601 // Possible null reference assignment.
            Post = await _context.Posts.FirstOrDefaultAsync(p => p.PostId == id);
#pragma warning restore CS8601 // Possible null reference assignment.

            if (Post == null)
            {
                return NotFound();
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            var existingPost = await _context.Posts.FindAsync(Post.PostId);
            if (existingPost == null)
                return NotFound();

            existingPost.Title = Post.Title;
            existingPost.Content = Post.Content;
            existingPost.Category = Post.Category;
            existingPost.Status = Post.Status;
            existingPost.ImageUrl = Post.ImageUrl;
            existingPost.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Đã cập nhật bài đăng '{existingPost.Title}' thành công.";
            return RedirectToPage("./Index");
        }
    }
}