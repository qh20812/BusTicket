using BusTicketSystem.Data;
using BusTicketSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
// using MySql.Data.MySqlClient; 
using System.ComponentModel.DataAnnotations;
using BusTicketSystem.ValidationAttributes;
using System.Threading.Tasks;

namespace BusTicketSystem.Pages.Account.DriverRegister
{
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(AppDbContext context, ILogger<IndexModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        [BindProperty]
        public DriverRegistrationInputModel Input { get; set; } = new DriverRegistrationInputModel();

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Check for existing license number
            if (await _context.Drivers.AnyAsync(d => d.LicenseNumber.ToLower() == Input.LicenseNumber.ToLower()))
            {
                ModelState.AddModelError("Input.LicenseNumber", "Số bằng lái này đã tồn tại trong hệ thống.");
                return Page();
            }

            // Optional: Check for existing email if it should be unique
            if (!string.IsNullOrEmpty(Input.Email) && await _context.Drivers.AnyAsync(d => d.Email != null && d.Email.ToLower() == Input.Email.ToLower()))
            {
                ModelState.AddModelError("Input.Email", "Địa chỉ email này đã tồn tại trong hệ thống.");
                return Page();
            }

            // Optional: Check for existing phone if it should be unique
            if (!string.IsNullOrEmpty(Input.Phone) && await _context.Drivers.AnyAsync(d => d.Phone != null && d.Phone == Input.Phone))
            {
                 ModelState.AddModelError("Input.Phone", "Số điện thoại này đã tồn tại trong hệ thống.");
                 return Page();
            }

            var newDriver = new Driver
            {
                Fullname = Input.Fullname,
                Email = Input.Email,
                Phone = Input.Phone,
                LicenseNumber = Input.LicenseNumber,
                DateOfBirth = Input.DateOfBirth,
                Address = Input.Address,
                Status = DriverStatus.PendingApproval, // New applications are pending approval
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                JoinedDate = null, // JoinedDate will be set upon approval
                Trips = new List<Trip>() // Initialize collection
            };

            _context.Drivers.Add(newDriver);

            // Create notification for admin
            var notification = new Notification
            {
                Message = $"Có hồ sơ ứng tuyển mới từ tài xế: '{newDriver.Fullname}'.",
                Category = NotificationCategory.Driver, // Assuming you have this enum category
                TargetUrl = Url.Page("/ForAdmin/DriverManage/Index", new { SearchName = newDriver.Fullname }),
                IconCssClass = "bi bi-person-plus-fill", // Icon for new application
                RecipientUserId = null, // Send to all admins or a specific admin role
                CreatedAt = DateTime.UtcNow
            };
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync(); // Dòng này có thể ném DbUpdateException
            TempData["SuccessMessage"] = "Đăng ký thành công! Hồ sơ của bạn đã được gửi để xem xét.";
            return RedirectToPage("/Index"); // Or a specific "Thank You" page
        }
    }

    public class DriverRegistrationInputModel
    {
        [Required(ErrorMessage = "Họ tên không được để trống.")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "Họ tên phải có từ 3 đến 100 ký tự.")]
        [RegularExpression(@"^[a-zA-ZÀÁÂÃÈÉÊÌÍÒÓÔÕÙÚĂĐĨŨƠàáâãèéêìíòóôõùúăđĩũơƯĂẠẢẤẦẨẪẬẮẰẲẴẶẸẺẼỀỀỂẾưăạảấầẩẫậắằẳẵặẹẻẽềềểếỄỆỈỊỌỎỐỒỔỖỘỚỜỞỠỢỤỦỨỪễệỉịọỏốồổỗộớờởỡợụủứừỬỮỰỲỴÝỶỸửữựỳỵýỷỹ\s]*$", ErrorMessage = "Họ tên chỉ được chứa chữ cái (bao gồm tiếng Việt) và khoảng trắng.")]
        [Display(Name = "Họ và tên")]
        public string Fullname { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "Địa chỉ email không hợp lệ.")]
        [StringLength(100)]
        [Display(Name = "Email")]
        public string? Email { get; set; }

        [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
        [StringLength(20, MinimumLength = 10, ErrorMessage = "Số điện thoại phải có từ 10 đến 20 ký tự.")]
        [RegularExpression(@"^(?!\-)[0-9\+\s\(\)-]*$", ErrorMessage = "Số điện thoại không được là giá trị âm.")]
        [Display(Name = "Số điện thoại")]
        public string? Phone { get; set; }

        [Required(ErrorMessage = "Số bằng lái không được để trống.")]
        [StringLength(20, MinimumLength = 5, ErrorMessage = "Số bằng lái phải có từ 5 đến 20 ký tự.")]
        [RegularExpression(@"^[a-zA-Z0-9]+$", ErrorMessage = "Số bằng lái chỉ được chứa chữ cái và số, không chứa ký tự đặc biệt, dấu cách hoặc dấu âm.")]
        [Display(Name = "Số bằng lái (CMTND/CCCD)")]
        public string LicenseNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ngày sinh không được để trống.")]
        [Display(Name = "Ngày sinh")]
        [DataType(DataType.Date)]
        [MinimumAge(24, ErrorMessage = "Tài xế phải ít nhất 24 tuổi để đăng ký lái xe buýt.")]
        public DateTime? DateOfBirth { get; set; }

        [StringLength(255, ErrorMessage = "Địa chỉ không được vượt quá 255 ký tự.")]
        [AddressFormat(ErrorMessage = "Địa chỉ không đúng định dạng. Vui lòng nhập theo mẫu: Xã/Phường, Quận/Huyện, Tỉnh/Thành phố. (Số nhà, tên đường có thể thêm vào phần đầu)")]
        [Display(Name = "Địa chỉ thường trú: (Số nhà, tên đường) Xã/Phường, Quận/Huyện, Tỉnh/Thành phố")]
        public string? Address { get; set; }
    }
}