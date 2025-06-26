using System.ComponentModel.DataAnnotations;
using BusTicketSystem.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BusTicketSystem.Helpers;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims; // Thêm using này
using Microsoft.AspNetCore.Authentication; // Thêm using này
using BCrypt.Net; // Đảm bảo using BCrypt.Net

namespace BusTicketSystem.Pages.Account.Login
{
    public class LoginModel : PageModel
    {
        private readonly AppDbContext _context;
        public LoginModel(AppDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }
        [BindProperty]
        public string CsrfToken { get; set; } = Guid.NewGuid().ToString();
        [BindProperty]
        [Required]
        public string Role { get; set; } = "Member";
        [BindProperty]
        [Required(ErrorMessage = "Bắt buộc phải nhập tên người dùng hoặc email.")]
        public string Username { get; set; } = string.Empty;
        [BindProperty]
        [Required(ErrorMessage = "Bắt buộc phải nhập mật khẩu.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
        public void OnGet() { }
        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                    ErrorMessage = "Vui lòng nhập đầy đủ và đúng thông tin. Lỗi: " + string.Join("; ", errors);
                    return Page();
                }
                // Tìm người dùng theo username hoặc email
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == Username || u.Email == Username);
                if (user == null)
                {
                    ErrorMessage = "Tên đăng nhập hoặc email không tồn tại.";
                    return Page();
                }

                if (!user.IsActive)
                {
                    ErrorMessage = "Tài khoản của bạn chưa được kích hoạt hoặc đã bị khóa. Vui lòng liên hệ quản trị viên.";
                    return Page();
                }

                bool passwordVerified = false;

                // 1. Thử xác thực bằng BCrypt trước
                // BCrypt.Verify sẽ ném ngoại lệ nếu hash không phải là định dạng BCrypt hợp lệ.
                // Hoặc trả về false nếu không khớp.
                try
                {
                    if (user.PasswordHash.StartsWith("$2a$") || user.PasswordHash.StartsWith("$2b$")) // Kiểm tra sơ bộ định dạng BCrypt
                    {
                        passwordVerified = BCrypt.Net.BCrypt.Verify(Password, user.PasswordHash);
                    }
                }
                catch (BCrypt.Net.SaltParseException) // Bắt lỗi nếu không phải định dạng BCrypt
                {
                    // Không làm gì, sẽ thử SHA256 ở dưới
                }

                // 2. Nếu BCrypt không thành công (hoặc không phải định dạng BCrypt), thử xác thực bằng SHA256 cũ
                if (!passwordVerified && user.PasswordHash.Contains(":"))
                {
                    var parts = user.PasswordHash.Split(':');
                    if (parts.Length == 2)
                    {
                        string storedSha256Hash = parts[0];
                        try
                        {
                            byte[] salt = Convert.FromBase64String(parts[1]);
                            string hashedInputPasswordWithOldSalt = HashPasswordWithSHA256(Password, salt);

                            if (hashedInputPasswordWithOldSalt == storedSha256Hash)
                            {
                                passwordVerified = true;
                                // Nâng cấp mật khẩu lên BCrypt
                                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(Password);
                                _context.Users.Update(user);
                                // Lưu thay đổi sẽ được thực hiện ở cuối nếu không có lỗi khác
                            }
                        }
                        catch (FormatException)
                        {
                            // Salt không phải base64 hợp lệ, coi như mật khẩu sai
                            ErrorMessage = "Dữ liệu mật khẩu không hợp lệ (salt).";
                            return Page();
                        }
                    }
                }

                if (!passwordVerified)
                {
                    ErrorMessage = "Mật khẩu không đúng.";
                    return Page();
                }

                // Tạo claims cho người dùng
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                    new Claim(ClaimTypes.Role, user.Role)
                    // Thêm các claim khác nếu cần, ví dụ Email
                    // new Claim(ClaimTypes.Email, user.Email),
                };

                if (user.Role.Trim().Equals("partner", StringComparison.OrdinalIgnoreCase) && user.CompanyId.HasValue)
                {
                    claims.Add(new Claim("CompanyId", user.CompanyId.Value.ToString())); // Sửa thành "CompanyId"
                }

                var claimsIdentity = new ClaimsIdentity(claims, "CookieAuth"); // "CookieAuth" là scheme bạn định nghĩa
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true, // Cookie sẽ tồn tại sau khi đóng trình duyệt nếu true
                    // ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(30) // Thời gian hết hạn cookie
                };

                await HttpContext.SignInAsync("CookieAuth", new ClaimsPrincipal(claimsIdentity), authProperties);

                // Vẫn có thể lưu một số thông tin vào session nếu cần truy cập nhanh mà không cần parse claim
                HttpContext.Session.SetUserId(user.UserId);
                HttpContext.Session.SetUserRole(user.Role.ToString());
                HttpContext.Session.SetString("Username", user.Username);
                HttpContext.Session.SetString("AvatarPath", user.AvatarPath ?? "/images/logo2.png"); // Thêm dòng này để lưu avatar vào session
                if (user.Role != null && user.Role.Trim().Equals("admin", StringComparison.OrdinalIgnoreCase))
                {
                    return RedirectToPage("/ForAdmin/Dashboard/Index");
                }
                else if (user.Role != null && user.Role.Trim().Equals("partner", StringComparison.OrdinalIgnoreCase))
                {
                    return RedirectToPage("/ForPartner/Dashboard/Index");
                }
                else
                {
                    return RedirectToPage("/Index");
                }
            }
            catch (Exception e)
            {
                ErrorMessage = $"Lỗi đăng nhập: {e.Message}";
                return Page();
            }
        }
        private string HashPasswordWithSHA256(string password, byte[] salt)
        {
            using (var sha256 = SHA256.Create())
            {
                byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
                byte[] saltedPassword = new byte[passwordBytes.Length + salt.Length];
                Buffer.BlockCopy(passwordBytes, 0, saltedPassword, 0, passwordBytes.Length);
                Buffer.BlockCopy(salt, 0, saltedPassword, passwordBytes.Length, salt.Length);
                byte[] hashedBytes = sha256.ComputeHash(saltedPassword);
                StringBuilder builder = new StringBuilder();
                foreach (byte b in hashedBytes)
                {
                    builder.Append(b.ToString("x2"));
                }
                return builder.ToString();
            }
        }
    }
}