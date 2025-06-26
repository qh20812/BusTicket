using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BusTicketSystem.Models
{
    public enum BusStatus
    {
        [Display(Name = "Chờ duyệt")]
        PendingApproval,
        [Display(Name = "Hoạt động.")]
        Active,
        [Display(Name = "Bảo trì.")]
        Maintenance,
        [Display(Name = "Ngừng hoạt động")]
        Inactive,
        [Display(Name = "Bị từ chối")]
        Rejected
    }
    public class Bus
    {
        [Key]
        public int BusId { get; set; }
        [Required]
        [StringLength(20)]
        public string LicensePlate { get; set; } = string.Empty;
        [Required]
        [StringLength(50)]
        public string BusType { get; set; } = string.Empty;
        [Required]
        public int? Capacity { get; set; }
        [Required]
        [Display(Name = "Trạng thái")]
        public BusStatus Status { get; set; } = BusStatus.PendingApproval; 
        // [Required] // CompanyId is nullable in DB
        public int? CompanyId { get; set; }
        public string RejectionReason { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        //relationship
        public BusCompany Company { get; set; }

        public ICollection<Trip> Trips { get; set; } // Gỡ bỏ 'required'

        public Bus()
        {
            Trips = new List<Trip>(); // Khởi tạo collection
        }
    }
}