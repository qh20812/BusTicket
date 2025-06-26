using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BusTicketSystem.Models
{
    public enum RouteStatus
    {
        [Display(Name = "Chờ duyệt")] PendingApproval,
        [Display(Name = "Đã duyệt")] Approved,
        [Display(Name = "Bị từ chối")] Rejected
    }

    public class Route
    {
        [Key]
        public int RouteId { get; set; }

        [Required]
        [StringLength(100)]
        public string Departure { get; set; } = string.Empty; // Tên địa điểm chung, ví dụ: "Bến xe Miền Đông"

        [Required]
        [StringLength(100)]
        public string Destination { get; set; } = string.Empty; // Tên địa điểm chung, ví dụ: "Bến xe Đà Lạt"

        [StringLength(255)]
        public string? OriginAddress { get; set; } // Địa chỉ chi tiết của điểm đi (cho Geocoding/hiển thị)

        [StringLength(255)]
        public string? DestinationAddress { get; set; } // Địa chỉ chi tiết của điểm đến

        [StringLength(50)]
        public string? OriginCoordinates { get; set; } // Format: "latitude,longitude"

        [StringLength(50)]
        public string? DestinationCoordinates { get; set; } // Format: "latitude,longitude"

        [Column(TypeName = "decimal(10,2)")] // Matches SQL DECIMAL(10,2)
        public decimal? Distance { get; set; }
        public TimeSpan? EstimatedDuration { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        [Required]
        public RouteStatus Status { get; set; } = RouteStatus.PendingApproval; // Default for new routes

        public int? ProposedByCompanyId { get; set; } // ID của nhà xe đã đề xuất lộ trình này
        public BusCompany? ProposedByCompany { get; set; }

        public required ICollection<Trip> Trips { get; set; }
        public required ICollection<Promotion> Promotions{ get; set; }

        public Route() // Thêm constructor
        {
            Trips = new List<Trip>();
            Promotions = new List<Promotion>();
        }
    }
}