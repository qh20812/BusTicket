using System.ComponentModel.DataAnnotations;

namespace BusTicketSystem.Models
{
    public class User
    {
        [Key]
        public int UserId { get; set; }
        [Required]
        [StringLength(50)]
        public string Username { get; set; } = string.Empty;
        [Required]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;
        [Required]
        [StringLength(255)]
        public string PasswordHash { get; set; } = string.Empty;
        [StringLength(100)]
        public string? Fullname { get; set; } = string.Empty;
        [StringLength(10)]
        [Phone]
        public string? Phone { get; set; } = string.Empty;
        public int? CompanyId { get; set; } // Added
        public string? Address { get; set; } = string.Empty;
        [Required]
        public string Role { get; set; } = "member"; // SQL ENUM('admin', 'member', 'partner')
        public string? AvatarPath { get; set; } // Đường dẫn tới ảnh đại diện
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true; 
        // public string? PasswordResetToken { get; set; }
        // public DateTime? PasswordResetTokenExpiry{ get; set; }
        public BusCompany? Company { get; set; } // Added navigation property
        public required ICollection<Ticket> Tickets { get; set; }
        public required ICollection<Order> Orders { get; set; }
        public required ICollection<Post> Posts { get; set; }
        public required ICollection<Notification> Notifications { get; set; }
        public required ICollection<LoginHistory> LoginHistory{ get; set; }
    }
}