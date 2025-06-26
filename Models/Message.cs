using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BusTicketSystem.Models
{
    public enum MessageStatus
    {
        [Display(Name = "Chưa đọc")]
        Unread = 0,

        [Display(Name = "Đã đọc")]
        Read = 1,

        [Display(Name = "Đã trả lời")]
        Replied = 2,
        [Display(Name = "Đã đóng")]
        Closed =3
    }
    // Removed extra closing brace here
    public class Message
    {
        [Key]
        public int MessageId { get; set; }
        // Fields for sender information.
        // These are stored directly for all messages, whether from a guest or a registered user.
        // If from a registered user (SenderId is not null), these can be pre-filled from User's details.
        [Required(ErrorMessage = "Tên người gửi không được để trống.")]
        [StringLength(100)]
        public string SenderName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email người gửi không được để trống.")]
        [StringLength(100)]
        [EmailAddress]
        public string SenderEmail { get; set; } = string.Empty;

        [StringLength(20)]
        [Phone]
        public string? SenderPhone { get; set; }

        [Required]
        [StringLength(255)] // Matches SQL VARCHAR(255)
        public string Subject { get; set; } = string.Empty;

        [Required]
        public string Content { get; set; } = string.Empty;

        public MessageStatus Status { get; set; } = MessageStatus.Unread;

        public DateTime SentAt { get; set; } = DateTime.UtcNow; // Renamed from CreatedAt, removed UpdatedAt

        // Navigation properties
        // Sender and Recipient direct user navigation removed as per new schema
        // Admin interaction fields
        [Column(TypeName = "TEXT")]
        public string? AdminNotes { get; set; }

        public DateTime? RepliedAt { get; set; }

        [ForeignKey("RepliedByUser")]
        public int? RepliedByUserId { get; set; }
        public virtual User? RepliedByUser { get; set; } // Admin who replied

        public DateTime? ClosedAt { get; set; }

        [ForeignKey("ClosedByUser")]
        public int? ClosedByUserId { get; set; }
        public virtual User? ClosedByUser { get; set; } // Admin who closed
    }
}