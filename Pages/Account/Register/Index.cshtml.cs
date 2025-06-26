using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BusTicketSystem.Data;
using BusTicketSystem.Models;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System;

namespace BusTicketSystem.Pages
{
    public class RegisterModel : PageModel
    {
        private readonly AppDbContext _context;
        public RegisterModel(AppDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        [BindProperty]
        public InputModel Input { get; set; } = new InputModel();
        public string ErrorMessage { get; set; } = string.Empty;
        // public string SuccessMessage { get; set; } = string.Empty; // Sẽ không dùng ở đây nữa

        public class InputModel
        {
            [Required(ErrorMessage = "Bắt buộc phải nhập tên người dùng.")]
            [StringLength(50, ErrorMessage = "Tên người dùng không được vượt quá 50 ký tự.")]
            [MinLength(5, ErrorMessage="Tên người dùng phải có ít nhất 5 ký tự.")]
            [RegularExpression(@"^[a-zA-Z0-9_]*$", ErrorMessage = "Tên người dùng không được chứa ký tự đặc biệt hoặc dấu.")]
            public string Username { get; set; } = string.Empty;
            [Required(ErrorMessage ="Bắt buộc phải nhập email.")]
            [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
            [StringLength(100, ErrorMessage = "Email không được vượt quá 100 ký tự.")]
            public string Email { get; set; } = string.Empty;

            [Required(ErrorMessage = "Bắt buộc phải nhập mật khẩu.")]
            [StringLength(255, ErrorMessage = "Mật khẩu không được vượt quá 255 ký tự.")]
            [MinLength(7, ErrorMessage = "Mật khẩu phải có ít nhất 7 ký tự.")]
            [DataType(DataType.Password)]
            public string Password { get; set; } = string.Empty;
            public string Fullname { get; set; } = string.Empty;
            public string Phone { get; set; } = string.Empty;
        }

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                ErrorMessage = "Vui lòng nhập đầy đủ và đúng thông tin. Lỗi: " + string.Join("; ", errors);
                return Page();
            }

            // Kiểm tra username, email hoặc số điện thoại đã tồn tại
            if (await _context.Users.AnyAsync(u => u.Username == Input.Username))
            {
                ErrorMessage = "Tên người dùng đã được sử dụng.";
                return Page();
            }
            if (await _context.Users.AnyAsync(u => u.Email == Input.Email))
            {
                ErrorMessage = "Email đã được sử dụng.";
                return Page();
            }
            

            // Tạo salt và băm mật khẩu
            byte[] salt = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }
            string hashedPassword = HashPasswordWithSHA256(Input.Password, salt);
            string saltBase64 = Convert.ToBase64String(salt);

            // Tạo đối tượng User
            var user = new User
            {
                Username = Input.Username,
                Email = Input.Email,
                PasswordHash = $"{hashedPassword}:{saltBase64}",
                Fullname = Input.Fullname,
                Role="Member",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Tickets = new List<Ticket>(),
                Orders = new List<Order>(),
                Posts = new List<Post>(),
                Notifications = new List<Notification>(),
                LoginHistory = new List<LoginHistory>()
            };
            // Lưu vào cơ sở dữ liệu
            _context.Users.Add(user);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Lỗi khi lưu dữ liệu: {ex.Message}";
                return Page();
            }

            // SuccessMessage = "Đăng ký thành công! Vui lòng đăng nhập vào tài khoản."; // Thay bằng TempData
            TempData["SuccessMessage"] = "Đăng ký thành công! Vui lòng đăng nhập vào tài khoản.";
            return RedirectToPage("/Account/Login/Login");
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