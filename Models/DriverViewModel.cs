using System;
using System.ComponentModel.DataAnnotations;
using BusTicketSystem.ValidationAttributes; // Thêm using này
using System.Reflection; 
namespace BusTicketSystem.Models
{
    public class DriverViewModel
    {
        public int DriverId { get; set; }
        public string Fullname { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string LicenseNumber { get; set; } = string.Empty;
        public DriverStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? JoinedDate { get; set; }
        public string? TerminationReason { get; set; }
        public string? CompanyName { get; set; }

        public string StatusDisplayName
        {
            get
            {
                var field = typeof(DriverStatus).GetField(Status.ToString());
                var displayAttribute = field?.GetCustomAttributes(typeof(DisplayAttribute), false).FirstOrDefault() as DisplayAttribute;
                return displayAttribute?.GetName() ?? Status.ToString();
            }
        }
        public DriverViewModel(Driver driver)
        {
            DriverId = driver.DriverId;
            Fullname = driver.Fullname;
            Email = driver.Email;
            Phone = driver.Phone;
            LicenseNumber = driver.LicenseNumber;
            Status = driver.Status;
            CreatedAt = driver.CreatedAt;
            JoinedDate = driver.JoinedDate;
            TerminationReason = driver.TerminationReason;
            CompanyName = driver.Company?.CompanyName;
        }
    }
    public class DriverActionInputModel
    {
        [Required]
        public int DriverIdToAction { get; set; }

        [Required(ErrorMessage = "Lý do không được để trống.")]
        [MinLength(10, ErrorMessage = "Lý do phải có ít nhất 10 ký tự.")]
        [StringLength(500, ErrorMessage = "Lý do không được vượt quá 500 ký tự.")]
        public string Reason { get; set; } = string.Empty;

        [Required]
        public string ActionType { get; set; } = string.Empty;
    }
    public class DriverInputModel 
    {
        public int DriverId { get; set; }

        [Required(ErrorMessage = "Họ tên không được để trống.")]
        [StringLength(100, ErrorMessage = "Họ tên không được vượt quá 100 ký tự.")]
        [RegularExpression(@"^[^\d\W].*$", ErrorMessage = "Họ tên phải bắt đầu bằng chữ cái.")] // Tên không bắt đầu bằng chữ số hoặc ký tự đặc biệt
        [Display(Name = "Họ và tên")]
        public string Fullname { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "Địa chỉ email không hợp lệ.")]
        [StringLength(100)]
        [Display(Name = "Email")]
        public string? Email { get; set; }

        [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
        [StringLength(15, MinimumLength = 10, ErrorMessage = "Số điện thoại phải có từ 10 đến 15 ký tự.")] // Giới hạn SĐT
        [RegularExpression(@"^(?!\-)[0-9\+\s\(\)-]*$", ErrorMessage = "Số điện thoại không hợp lệ và không được là số âm.")] // Không cho giá trị âm và ký tự không hợp lệ
        [Display(Name = "Số điện thoại")]
        public string? Phone { get; set; }

        [Required(ErrorMessage = "Số bằng lái không được để trống.")]
        [StringLength(20, MinimumLength = 10, ErrorMessage = "Số bằng lái phải có từ 10 đến 20 ký tự.")] // Giới hạn số bằng lái
        [RegularExpression(@"^[a-zA-Z0-9]+$", ErrorMessage = "Số bằng lái chỉ được chứa chữ cái và số, không được là giá trị âm hoặc chứa ký tự đặc biệt.")] // Không cho giá trị âm và ký tự đặc biệt
        [Display(Name = "Số bằng lái")]
        public string LicenseNumber { get; set; } = string.Empty;

        [Display(Name = "Ngày sinh")]
        [DataType(DataType.Date)]
        [MinimumAge(24, ErrorMessage = "Tài xế phải ít nhất 24 tuổi.")] // Thêm dòng này
        public DateTime? DateOfBirth { get; set; }

        [StringLength(255, MinimumLength = 10, ErrorMessage = "Địa chỉ phải có ít nhất 10 ký tự và không vượt quá 255 ký tự.")] // Giới hạn địa chỉ
        [AddressFormat(ErrorMessage = "Địa chỉ không đúng định dạng. Vui lòng nhập theo mẫu: (Số nhà, tên đường), Xã/Phường, Quận/Huyện, Tỉnh/Thành phố.")] // Thêm validation cho địa chỉ
        [Display(Name = "Địa chỉ")]
        public string? Address { get; set; }

        [Required]
        [Display(Name = "Trạng thái")]
        public DriverStatus Status { get; set; }

        [Display(Name = "Công ty")]
        public int? CompanyId { get; set; }
    }
}