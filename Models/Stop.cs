using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BusTicketSystem.Models
{
    public enum StopStatus
    {
        [Display(Name = "Chờ duyệt")]
        PendingApproval,
        [Display(Name = "Đã duyệt")]
        Approved,
        [Display(Name = "Bị từ chối")]
        Rejected
    }

    public class Stop
    {
        [Key]
        public int StopId { get; set; }

        [Required(ErrorMessage = "Tên trạm dừng không được để trống.")]
        [StringLength(255, ErrorMessage = "Tên trạm dừng không được vượt quá 255 ký tự.")]
        [Display(Name = "Tên trạm dừng")]
        public string StopName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vĩ độ không được để trống.")]
        [Range(-90.0, 90.0, ErrorMessage = "Vĩ độ không hợp lệ.")]
        [Display(Name = "Vĩ độ")]
        [Column(TypeName = "double")]
        public double Latitude { get; set; }

        [Required(ErrorMessage = "Kinh độ không được để trống.")]
        [Range(-180.0, 180.0, ErrorMessage = "Kinh độ không hợp lệ.")]
        [Display(Name = "Kinh độ")]
        [Column(TypeName = "double")]
        public double Longitude { get; set; }

        [StringLength(500, ErrorMessage = "Địa chỉ không được vượt quá 500 ký tự.")]
        [Display(Name = "Địa chỉ")]
        public string? Address { get; set; }

        public int? CompanyId { get; set; }
        [ForeignKey("CompanyId")]
        public BusCompany? Company { get; set; }

        [Required]
        [Display(Name = "Trạng thái")]
        public StopStatus Status { get; set; } = StopStatus.PendingApproval;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}