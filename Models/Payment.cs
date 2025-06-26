using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BusTicketSystem.Models
{
    public enum PaymentGateway
    {
        MoMo,
        VNPay,
        ZaloPay,
        PayPal,
        BankTransfer
    }
    public enum PaymentStatus
    {
        Pending,
        Success,
        Failed,
        Refunded
    }
    public class Payment
    {
        [Key]
        public int PaymentId { get; set; }
        [Required]
        public int OrderId { get; set; }
        [Required] // SQL length is 100
        [StringLength(100)]
        public string TransactionId { get; set; } = string.Empty;
        [Required]
        public PaymentGateway PaymentGateway { get; set; }
        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal Amount { get; set; }
        [Required]
        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
        [Required]
        public DateTime TransactionTime { get; set; }
        [StringLength(50)] // Nullable in SQL as it's not NOT NULL
        public string? ErrorCode { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public string? PaymentContent { get; set; } // Added (TEXT in DB)

        public Order? Order { get; set; }
    }
}