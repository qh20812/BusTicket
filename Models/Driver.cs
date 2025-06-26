using System.ComponentModel.DataAnnotations;

namespace BusTicketSystem.Models
{
    public enum DriverStatus
    { 
        [Display(Name = "Chờ duyệt")]
        PendingApproval = 0,
        [Display(Name = "Đang xem xét")] 
        UnderReview = 1,
        [Display(Name = "Đang hoạt động")] 
        Active = 2,
        [Display(Name = "Tạm nghỉ")] 
        OnLeave = 3,
        [Display(Name = "Đã nghỉ việc")] 
        Resigned = 4,
        [Display(Name = "Đã sa thải")] 
        Terminated = 5,
        [Display(Name = "Đã từ chối")] 
        Rejected = 6
    }
    public class Driver
    {
        [Key]
        public int DriverId { get; set; }
        [Required]
        [StringLength(100)]
        public string Fullname { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "Địa chỉ email không hợp lệ.")]
        [StringLength(100)]
        public string? Email { get; set; }

        [StringLength(20)] 
        [Phone]
        public string? Phone { get; set; } 

        [Required]
        [StringLength(20)]
        public string LicenseNumber { get; set; } = string.Empty;

        [Display(Name = "Ngày sinh")]
        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }
        
        [StringLength(255, ErrorMessage = "Địa chỉ không được vượt quá 255 ký tự.")]
        public string? Address { get; set; }

        public int? CompanyId { get; set; } // Changed to nullable to match DB

        [Required]
        [Display(Name = "Trạng thái")]
        public DriverStatus Status { get; set; } = DriverStatus.PendingApproval; // Match SQL default or common initial state

        [Display(Name = "Ngày tham gia")]
        [DataType(DataType.Date)]
        public DateTime? JoinedDate { get; set; } 

        [Display(Name = "Ngày sa thải/nghỉ việc")]
        [DataType(DataType.Date)]
        public DateTime? TerminationDate { get; set; } // Đổi tên

        [Display(Name = "Lý do sa thải/nghỉ việc")]
        [StringLength(500, ErrorMessage = "Lý do không vượt quá 500 ký tự")]
        public string? TerminationReason { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        //relationship
        public BusCompany? Company { get; set; } // Changed to nullable
        public ICollection<Trip> Trips { get; set; } = new List<Trip>(); // Initialize and remove 'required' if EF Core handles it, or ensure it's set on creation
    }
}