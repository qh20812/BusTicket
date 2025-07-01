using System.ComponentModel.DataAnnotations;

namespace BusTicketSystem.Models
{
    public enum BusCompanyStatus
    {
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
        public string? Address { get; set; } = string.Empty;
        [StringLength(20)]
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
        public BusCompanyStatus Status { get; set; } = BusCompanyStatus.PendingApproval;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public string? RejectionReason { get; set; }
        public DateTime? RejectedAt { get; set; }
        public string? TerminationReason { get; set; }
        public DateTime? TerminatedAt { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public DateTime? ApprovedAt { get; set; }

        public ICollection<Bus> Buses { get; set; }
        public ICollection<Driver> Drivers { get; set; }
        public ICollection<Trip> Trips { get; set; }
        public ICollection<Stop> Stops { get; set; } // Thêm mối quan hệ

        public BusCompany()
        {
            Buses = new List<Bus>();
            Drivers = new List<Driver>();
            Trips = new List<Trip>();
            Stops = new List<Stop>();
        }
    }
}