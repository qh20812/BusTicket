using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BusTicketSystem.Models
{
    public enum CancellationRequestStatus
    {
        [Display(Name = "Chờ duyệt")]
        PendingApproval,
        [Display(Name = "Đã duyệt")]
        Approved,
        [Display(Name = "Đã từ chối")]
        Rejected
    }

    public class Cancellation
    {
        [Key]
        public int CancellationId { get; set; }
        [Required]
        public int TicketId { get; set; }
        [Required]
        public int OrderId { get; set; }

        [Required(ErrorMessage = "Lý do hủy vé không được để trống.")]
        [StringLength(500, ErrorMessage = "Lý do hủy không được quá 500 ký tự.")]
        public string Reason { get; set; } = string.Empty;

        [Column(TypeName = "decimal(10,2)")]
        public decimal? RefundedAmount { get; set; }

        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ProcessedAt { get; set; } // When admin approved/rejected

        [Required]
        public CancellationRequestStatus Status { get; set; } = CancellationRequestStatus.PendingApproval;

        [StringLength(500)]
        public string? AdminNotesOrRejectionReason { get; set; } // Admin's notes or reason for rejection

        public int? ProcessedByAdminId { get; set; } // Admin who processed
        [ForeignKey("ProcessedByAdminId")]
        public User? ProcessedByAdmin { get; set; }

        public Ticket? Ticket { get; set; }
        public Order? Order { get; set; }
    }
}