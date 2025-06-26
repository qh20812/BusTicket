using System;
using System.ComponentModel.DataAnnotations;

namespace BusTicketSystem.Models
{
    public enum NotificationCategory
    {
        [Display(Name = "Hệ thống")]
        System,
        [Display(Name = "Khách hàng")]
        Customer,
        [Display(Name = "Nhân sự")] // Dành cho Tài xế
        Driver,
        [Display(Name = "Đơn hàng")]
        Order,
        [Display(Name = "Đăng ký mới")]
        Registration,
        [Display(Name = "Bài đăng")]
        Post,
        [Display(Name = "Nhà xe")]
        BusCompany,
        [Display(Name = "Chuyến xe")]
        Trip,
        [Display(Name = "Phương tiện")]
        BusManagement
    }

    public class Notification
    {
        public int NotificationId { get; set; }

        public int? RecipientUserId { get; set; } // Renamed from AdminRecipientId
        public virtual User? RecipientUser { get; set; } // Renamed from AdminRecipient

        public int? RecipientCompanyId { get; set; } // ID của nhà xe nhận thông báo
        public virtual BusCompany? RecipientCompany { get; set; } // Navigation property cho nhà xe
        
        [Required(ErrorMessage = "Nội dung thông báo không được để trống.")]
        [StringLength(500, ErrorMessage = "Nội dung thông báo không quá 500 ký tự.")]
        public string Message { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsRead { get; set; } = false;
        public DateTime? ReadAt { get; set; } // Thời điểm đánh dấu đã đọc

        [Required]
        public NotificationCategory Category { get; set; }

        [StringLength(255)]
        public string? TargetUrl { get; set; } 

        [StringLength(50)]
        public string? IconCssClass { get; set; } 

    }
}