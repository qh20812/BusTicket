using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BusTicketSystem.Models
{
    public class TripStop
    {
        [Key]
        public int StopId { get; set; }

        [Required]
        public int TripId { get; set; }
        [ForeignKey("TripId")]
        public Trip? Trip { get; set; } // Navigation property

        [Required(ErrorMessage = "Thứ tự dừng không được để trống.")]
        [Display(Name = "Thứ tự dừng")]
        [Range(1, int.MaxValue, ErrorMessage = "Thứ tự dừng phải lớn hơn 0.")]
        public int StopOrder { get; set; } // Thứ tự của điểm dừng trong chuyến đi

        [Required(ErrorMessage = "Tên trạm dừng không được để trống.")]
        [StringLength(255)]
        [Display(Name = "Tên trạm dừng")]
        public string StationName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vĩ độ không được để trống.")]
        [Display(Name = "Vĩ độ")]
        [Column(TypeName = "double")] // Đảm bảo kiểu dữ liệu chính xác trong DB
        public double Latitude { get; set; }

        [Required(ErrorMessage = "Kinh độ không được để trống.")]
        [Display(Name = "Kinh độ")]
        [Column(TypeName = "double")] // Đảm bảo kiểu dữ liệu chính xác trong DB
        public double Longitude { get; set; }

        [Display(Name = "Thời gian đến dự kiến")]
        [DataType(DataType.Time)]
        public TimeSpan? EstimatedArrival { get; set; }

        [Display(Name = "Thời gian đi dự kiến")]
        [DataType(DataType.Time)]
        public TimeSpan? EstimatedDeparture { get; set; }

        [StringLength(500)]
        [Display(Name = "Ghi chú")]
        public string? Note { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}