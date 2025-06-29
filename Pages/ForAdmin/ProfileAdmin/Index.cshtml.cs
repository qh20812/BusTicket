using BusTicketSystem.Data;
using BusTicketSystem.Models;
using BusTicketSystem.Models.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Org.BouncyCastle.Crypto.Generators;
using System.Linq;
using BCrypt.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace BusTicketSystem.Pages.ForAdmin.ProfileAdmin
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public IndexModel(AppDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        [BindProperty]
        public ProfileAdminViewModel Profile { get; set; } = new ProfileAdminViewModel();

        [TempData]
        public string? SuccessMessage { get; set; }
        [TempData]
        public string? ErrorMessage { get; set; }

        private async Task<User?> GetCurrentUserAsync()
        {
            var username = _httpContextAccessor.HttpContext?.Session.GetString("Username");
            if (string.IsNullOrEmpty(username))
            {
                return null;
            }
            // Ensure we are fetching an admin user
            return await _context.Users.FirstOrDefaultAsync(u => u.Username == username && u.Role == "Admin");
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
            {
                // Redirect to login if user not found/not admin or session expired
                return RedirectToPage("/Account/Login/Index");
            }

            Profile.UserId = user.UserId;
            Profile.Username = user.Username;
            Profile.Email = user.Email;
            Profile.Fullname = user.Fullname;
            Profile.Phone = user.Phone;
            Profile.Address = user.Address;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var userToUpdate = await _context.Users.FindAsync(Profile.UserId);
            if (userToUpdate == null || userToUpdate.Role != "Admin")
            {
                ErrorMessage = "Không tìm thấy tài khoản hoặc bạn không có quyền cập nhật."; // Account not found or you do not have permission to update.
                return RedirectToPage();
            }
            // Repopulate Username for display if validation fails, as it's readonly in the form
            Profile.Username = userToUpdate.Username;

            if (!ModelState.IsValid)
            {
                return Page();
            }

            if (userToUpdate.Email != Profile.Email && await _context.Users.AnyAsync(u => u.Email == Profile.Email && u.UserId != userToUpdate.UserId))
            {
                ModelState.AddModelError("Profile.Email", "Địa chỉ email này đã được sử dụng."); // This email address is already in use.
                return Page();
            }

            userToUpdate.Email = Profile.Email;
            userToUpdate.Fullname = Profile.Fullname;
            userToUpdate.Phone = Profile.Phone;
            userToUpdate.Address = Profile.Address;
            userToUpdate.UpdatedAt = System.DateTime.UtcNow;

            if (!string.IsNullOrWhiteSpace(Profile.NewPassword))
            {
                userToUpdate.PasswordHash = global::BCrypt.Net.BCrypt.HashPassword(Profile.NewPassword);
            }

            _context.Users.Update(userToUpdate);
            await _context.SaveChangesAsync();
            SuccessMessage = "Thông tin cá nhân đã được cập nhật thành công."; // Personal information updated successfully.

            return RedirectToPage();
        }
    }
}