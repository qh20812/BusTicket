using System.ComponentModel.DataAnnotations;

namespace BusTicketSystem.Models
{
    public enum BusCompanyStatus
    { // SQL: 0: PendingApproval, 1: UnderReview, 2: Active, 3: Inactive, 4: Rejected, 5: Terminated
        [Display(Name = "Chờ duyệt")]
        PendingApproval = 0,
        [Display(Name = "Đang xem xét")]
        UnderReview = 1,
        [Display(Name = "Hoạt động")]
        Active = 2,
        [Display(Name = "Không hoạt động")]
        Inactive = 3,
        [Display(Name = "Bị từ chối")]
        Rejected = 4,
        [Display(Name = "Đã hủy hợp tác")]
        Terminated = 5
    }
    public class BusCompany
    {
        [Key]
        public int CompanyId { get; set; }
        [Required]
        [StringLength(100)]
        public string CompanyName { get; set; } = string.Empty;
        public string? Address { get; set; } =  string.Empty;
        [StringLength(20)] // Tăng giới hạn để khớp với DB VARCHAR(20)
        [Phone]
        public string? Phone { get; set; } = string.Empty;
        [StringLength(100)]
        [EmailAddress]
        public string? Email { get; set; } = string.Empty;
        public string? Description { get; set; } = string.Empty;

        [Display(Name = "Tên người liên hệ")]
        [StringLength(100)]
        public string? ContactPersonName { get; set; }

        [Display(Name = "Email người liên hệ")]
        [EmailAddress]
        [StringLength(100)]
        public string? ContactPersonEmail { get; set; }

        [Display(Name = "SĐT người liên hệ")]
        [Phone]
        [StringLength(20)]
        public string? ContactPersonPhone { get; set; }

        [Required]
        public BusCompanyStatus Status { get; set; } = BusCompanyStatus.PendingApproval; // Sẽ đặt giá trị cụ thể khi tạo
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public string? RejectionReason { get; set; }
        public DateTime? RejectedAt { get; set; }
        public string? TerminationReason { get; set; }
        public DateTime? TerminatedAt { get; set; }
        public DateTime? ReviewedAt { get; set; } // Ngày admin bắt đầu xem xét
        public DateTime? ApprovedAt { get; set; } // Ngày chính thức hoạt động

        public ICollection<Bus> Buses { get; set; } // Gỡ bỏ 'required'
        public ICollection<Driver> Drivers { get; set; } // Gỡ bỏ 'required'
        public ICollection<Trip> Trips { get; set; } // Gỡ bỏ 'required'

        public BusCompany()
        {
            Buses = new List<Bus>();
            Drivers = new List<Driver>();
            Trips = new List<Trip>();
        }
    }
}