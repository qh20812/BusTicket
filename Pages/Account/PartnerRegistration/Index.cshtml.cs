using BusTicketSystem.Data;
using BusTicketSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace BusTicketSystem.Pages.Account.PartnerRegistration
{
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;

        public IndexModel(AppDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public PartnerInputModel Input { get; set; } = new PartnerInputModel();

        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }

        public void OnGet()
        {
            ViewData["Title"] = "Đăng ký Đối tác Nhà xe";
            // Có thể thêm logic khởi tạo nếu cần
        }

        public async Task<IActionResult> OnPostAsync()
        {
            ViewData["Title"] = "Đăng ký Đối tác Nhà xe";
            if (!ModelState.IsValid)
            {
                // Gộp các lỗi ModelState vào ErrorMessage để hiển thị chung
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                ErrorMessage = "Vui lòng kiểm tra lại thông tin đã nhập. Lỗi: " + string.Join("; ", errors);
                return Page();
            }

            // Kiểm tra tên công ty hoặc email đã tồn tại chưa (chỉ kiểm tra với các công ty không bị từ chối/hủy)
            if (await _context.BusCompanies.AnyAsync(bc => bc.CompanyName == Input.CompanyName && bc.Status != BusCompanyStatus.Rejected && bc.Status != BusCompanyStatus.Terminated))
            {
                ModelState.AddModelError("Input.CompanyName", "Tên công ty này đã được đăng ký hoặc đang hoạt động.");
                return Page();
            }
            if (await _context.BusCompanies.AnyAsync(bc => bc.Email == Input.Email && bc.Status != BusCompanyStatus.Rejected && bc.Status != BusCompanyStatus.Terminated))
            {
                ModelState.AddModelError("Input.Email", "Email công ty này đã được đăng ký hoặc đang hoạt động.");
                return Page();
            }

            // Kiểm tra xem email công ty đã được sử dụng để tạo tài khoản người dùng chưa
            if (await _context.Users.AnyAsync(u => u.Username == Input.Email || u.Email == Input.Email))
            {
                ModelState.AddModelError("Input.Email", "Email này đã được sử dụng để đăng ký tài khoản người dùng. Vui lòng sử dụng email khác cho công ty hoặc liên hệ quản trị viên.");
                return Page();
            }

            if (Input.Password != Input.ConfirmPassword)
            {
                ModelState.AddModelError("Input.ConfirmPassword", "Mật khẩu xác nhận không khớp.");
                return Page();
            }

            var newCompany = new BusCompany
            {
                CompanyName = Input.CompanyName,
                Address = Input.Address,
                Phone = Input.Phone,
                Email = Input.Email,
                Description = Input.Description,
                ContactPersonName = Input.ContactPersonName,
                ContactPersonEmail = Input.ContactPersonEmail,
                ContactPersonPhone = Input.ContactPersonPhone,
                Status = BusCompanyStatus.PendingApproval, // Trạng thái chờ duyệt
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Bắt đầu một transaction để đảm bảo cả công ty và người dùng được tạo hoặc không gì cả
            using var transaction = await _context.Database.BeginTransactionAsync();

            _context.BusCompanies.Add(newCompany);
            await _context.SaveChangesAsync(); // Lưu để lấy CompanyId

            // Tạo salt và băm mật khẩu
            byte[] salt = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }
            string hashedPassword = HashPasswordWithSHA256(Input.Password, salt);
            string saltBase64 = Convert.ToBase64String(salt);

            var partnerUser = new User
            {
                Username = Input.Email, // Username là email công ty
                Email = Input.Email,    // Email người dùng cũng là email công ty
                PasswordHash = $"{hashedPassword}:{saltBase64}",
                Fullname = Input.ContactPersonName, // Lấy tên người liên hệ làm Fullname
                Phone = Input.ContactPersonPhone,   // Lấy SĐT người liên hệ
                Role = "Partner",
                CompanyId = newCompany.CompanyId, 
                IsActive = false, // <<<< THAY ĐỔI Ở ĐÂY: Tài khoản chưa kích hoạt cho đến khi admin duyệt
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Tickets = new List<Ticket>(),
                Orders = new List<Order>(),
                Posts = new List<Post>(),
                Notifications = new List<Notification>(),
                LoginHistory = new List<LoginHistory>()
            };
            _context.Users.Add(partnerUser);

            // Tạo thông báo cho admin
            var notification = new Notification
            {
                Message = $"Nhà xe '{newCompany.CompanyName}' vừa gửi yêu cầu đăng ký hợp tác.",
                Category = NotificationCategory.BusCompany,
                TargetUrl = Url.Page("/ForAdmin/BusCompanyManage/Index"), // Admin sẽ thấy trong danh sách chờ
                IconCssClass = "bi bi-building-add" // Icon cho đăng ký nhà xe mới
            };
            _context.Notifications.Add(notification);

            try
            {
                await _context.SaveChangesAsync(); // Lưu người dùng và thông báo
                await transaction.CommitAsync();

                SuccessMessage = "Yêu cầu đăng ký đối tác và tài khoản quản lý của bạn đã được tạo thành công! Chúng tôi sẽ sớm liên hệ với bạn.";
                Input = new PartnerInputModel(); // Reset form
                ModelState.Clear();
                return Page();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                // Log lỗi ex chi tiết
                ErrorMessage = "Đã xảy ra lỗi trong quá trình đăng ký. Vui lòng thử lại. Lỗi: " + ex.Message;
                // Cần xóa newCompany nếu đã được lưu ở SaveChangesAsync() đầu tiên mà User bị lỗi
                // Tuy nhiên, với transaction, nếu User lỗi thì newCompany cũng sẽ được rollback.
                // Nếu không dùng transaction, bạn cần xử lý xóa newCompany thủ công ở đây.
                var companyToRemove = await _context.BusCompanies.FindAsync(newCompany.CompanyId);
                if (companyToRemove != null)
                {
                    _context.BusCompanies.Remove(companyToRemove);
                    await _context.SaveChangesAsync(); // Clean up company if user creation failed
                }
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
                return BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();
            }
        }
    }

    public class PartnerInputModel
    {
        [Required(ErrorMessage = "Tên công ty không được để trống.")]
        [StringLength(100, MinimumLength = 5, ErrorMessage = "Tên công ty phải có từ 5 đến 100 ký tự.")]
        [RegularExpression(@"^[^\d].*", ErrorMessage = "Tên công ty không được bắt đầu bằng chữ số.")]
        public string CompanyName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Địa chỉ không được để trống.")]
        [StringLength(255, MinimumLength = 10, ErrorMessage = "Địa chỉ công ty phải có ít nhất 10 ký tự.")]
        public string Address { get; set; } = string.Empty;

        [Required(ErrorMessage = "Số điện thoại công ty không được để trống.")]
        [Phone(ErrorMessage = "Số điện thoại công ty không hợp lệ.")]
        [StringLength(20, MinimumLength = 10, ErrorMessage = "Số điện thoại công ty phải có ít nhất 10 ký tự.")]
        [RegularExpression(@"^(?!\-)[0-9\+\s\(\)-]*$", ErrorMessage = "Số điện thoại không hợp lệ. Không được bắt đầu bằng '-' và chỉ được chứa các ký tự số, '+', khoảng trắng, '(', ')', '-'.")]
        public string Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email công ty không được để trống.")]
        [EmailAddress(ErrorMessage = "Email công ty không hợp lệ.")]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required(ErrorMessage = "Tên người liên hệ không được để trống.")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "Tên người liên hệ phải có ít nhất 3 ký tự.")]
        [RegularExpression(@"^[^\d].*", ErrorMessage = "Tên người liên hệ không được bắt đầu bằng chữ số.")]
        public string ContactPersonName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email người liên hệ không được để trống.")]
        [EmailAddress(ErrorMessage = "Email người liên hệ không hợp lệ.")]
        [StringLength(100)]
        public string ContactPersonEmail { get; set; } = string.Empty;

        [Required(ErrorMessage = "SĐT người liên hệ không được để trống.")]
        [Phone(ErrorMessage = "SĐT người liên hệ không hợp lệ.")]
        [StringLength(20, MinimumLength = 10, ErrorMessage = "SĐT người liên hệ phải có ít nhất 10 ký tự.")]
        [RegularExpression(@"^(?!\-)[0-9\+\s\(\)-]*$", ErrorMessage = "Số điện thoại không hợp lệ. Không được bắt đầu bằng '-' và chỉ được chứa các ký tự số, '+', khoảng trắng, '(', ')', '-'.")]
        public string ContactPersonPhone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mật khẩu không được để trống.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự.")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu cho tài khoản đối tác")]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Xác nhận mật khẩu")]
        [Compare("Password", ErrorMessage = "Mật khẩu và mật khẩu xác nhận không khớp.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}