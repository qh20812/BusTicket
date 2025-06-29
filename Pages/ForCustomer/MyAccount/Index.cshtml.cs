using BusTicketSystem.Data;
using BusTicketSystem.Helpers;
using BusTicketSystem.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using BCrypt.Net;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using Microsoft.AspNetCore.Authorization;

namespace BusTicketSystem.Pages.ForCustomer.MyAccount
{
    [Authorize(Roles = "Member")]
#pragma warning disable CS9113 // Parameter is unread.
    public class IndexModel(AppDbContext context, IHttpContextAccessor httpContextAccessor, IWebHostEnvironment hostEnvironment) : PageModel
#pragma warning restore CS9113 // Parameter is unread.
    {
        private readonly AppDbContext _context = context;
        private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
        private readonly IWebHostEnvironment _hostEnvironment = hostEnvironment; // Gán giá trị cho _hostEnvironment
        [BindProperty]
        public ProfileCustomerViewModel Profile { get; set; } = new ProfileCustomerViewModel();

        [TempData]
        public string? SuccessMessage { get; set; }
        [TempData]
        public string? ErrorMessage { get; set; }

        private async Task<User?> GetCurrentUserAsync()
        {
            var userId = _httpContextAccessor.HttpContext?.Session.GetUserId();
            if (!userId.HasValue)
            {
                return null;
            }
            return await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId.Value && u.Role == "Member");
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
            {
                // Redirect to login if user not found/not member or session expired
                return RedirectToPage("/Account/Login/Login");
            }

            Profile.UserId = user.UserId;
            Profile.Username = user.Username;
            Profile.Email = user.Email;
            Profile.Fullname = user.Fullname;
            Profile.Phone = user.Phone;
            Profile.Address = user.Address;
            Profile.CurrentAvatarPath = user.AvatarPath ?? "/images/logo2.png"; // Sử dụng logo2.png làm mặc định

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var userToUpdate = await _context.Users.FindAsync(Profile.UserId);
            if (userToUpdate == null || userToUpdate.Role != "Member")
            {
                ErrorMessage = "Không tìm thấy tài khoản hoặc bạn không có quyền cập nhật.";
                return RedirectToPage();
            }
            Profile.Username = userToUpdate.Username;
            Profile.CurrentAvatarPath = userToUpdate.AvatarPath ?? "/images/logo2.png"; // Sử dụng logo2.png làm mặc định

            if (!ModelState.IsValid)
            {
                return Page();
            }

            if (userToUpdate.Email != Profile.Email && await _context.Users.AnyAsync(u => u.Email == Profile.Email && u.UserId != userToUpdate.UserId))
            {
                ModelState.AddModelError("Profile.Email", "Địa chỉ email này đã được sử dụng.");
                return Page();
            }

            userToUpdate.Email = Profile.Email;
            userToUpdate.Fullname = Profile.Fullname;
            userToUpdate.Phone = Profile.Phone;
            userToUpdate.Address = Profile.Address;
            userToUpdate.UpdatedAt = System.DateTime.UtcNow;

            if (!string.IsNullOrWhiteSpace(Profile.NewPassword))
            {
                userToUpdate.PasswordHash = BCrypt.Net.BCrypt.HashPassword(Profile.NewPassword);
            }

            if (Profile.AvatarFile != null && Profile.AvatarFile.Length > 0)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var extension = Path.GetExtension(Profile.AvatarFile.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(extension))
                {
                    ModelState.AddModelError("Profile.AvatarFile", "Chỉ cho phép tải lên ảnh có định dạng JPG, JPEG, PNG, GIF.");
                    return Page();
                }
                if (Profile.AvatarFile.Length > 2 * 1024 * 1024)
                {
                    ModelState.AddModelError("Profile.AvatarFile", "Kích thước ảnh không được vượt quá 2MB.");
                    return Page();
                }
                if (!string.IsNullOrEmpty(userToUpdate.AvatarPath) && userToUpdate.AvatarPath != "/images/logo2.png")
                {
                    var oldAvatarPhysicalPath = Path.Combine(_hostEnvironment.WebRootPath, userToUpdate.AvatarPath.TrimStart('/'));
                    if (System.IO.File.Exists(oldAvatarPhysicalPath))
                    {
                        System.IO.File.Delete(oldAvatarPhysicalPath);
                    }
                }
                var uploadsFolder = Path.Combine(_hostEnvironment.WebRootPath, "images", "avatars");
                Directory.CreateDirectory(uploadsFolder);
                var uniqueFileName = $"{userToUpdate.UserId}_{Guid.NewGuid()}{extension}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await Profile.AvatarFile.CopyToAsync(stream);
                }
                userToUpdate.AvatarPath = $"/images/avatars/{uniqueFileName}";
                _httpContextAccessor.HttpContext?.Session.SetString("AvatarPath", userToUpdate.AvatarPath);
            }

            _context.Users.Update(userToUpdate);
            await _context.SaveChangesAsync();
            SuccessMessage = "Thông tin cá nhân đã được cập nhật thành công.";

            return RedirectToPage();
        }
    }
}