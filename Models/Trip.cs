using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BusTicketSystem.Models
{
    public enum TripStatus
    {
        [Display(Name = "Chờ duyệt")]
        PendingApproval,
        [Display(Name = "Đã lên lịch")]
        Scheduled,
        [Display(Name = "Đã hủy")]
        Cancelled,
        [Display(Name = "Đã hoàn thành")]
        Completed,
        [Display(Name = "Bị từ chối")]
        Rejected
    }
    public class Trip
    {
        [Key]
        public int TripId { get; set; }
        [Required]
        public int RouteId { get; set; }
        [Required]
        public int BusId { get; set; }
        [Required]
        public int DriverId { get; set; }
        public int? CompanyId { get; set; }
        [Required]
        [Display(Name = "Thời gian khởi hành")]
        public DateTime DepartureTime { get; set; }
        [Required]
        [Column(TypeName = "decimal(10,2)")] // Matches SQL DECIMAL(10,2)
        [Display(Name = "Giá vé")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Giá vé phải lớn hơn 0.")]
        public decimal Price { get; set; }
        [Display(Name = "Số ghế trống")]
        [Range(0, int.MaxValue, ErrorMessage = "Số ghế trống không hợp lệ.")]
        public int? AvailableSeats { get; set; }
        [Required]
        [Display(Name = "Trạng thái")]
        public TripStatus Status { get; set; } = TripStatus.Scheduled;
        [StringLength(500)]
        [Display(Name = "Lý do hủy")]
        public string? CancellationReason { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        //relationship
        public Route Route { get; set; }
        public Bus Bus { get; set; }
        public Driver Driver { get; set; }
        public BusCompany Company { get; set; }
        public ICollection<Ticket> Tickets{ get; set; }=new List<Ticket>();
        public ICollection<TripStop> TripStops { get; set; } = new List<TripStop>(); // Thêm dòng này
    }
}