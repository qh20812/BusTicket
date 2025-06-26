using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BusTicketSystem.Models
{
    public enum TripType
    {
        [Display(Name ="Một chiều")]
        OneWay,
        [Display(Name ="Hai chiều")]
        RoundTrip
    }
    public enum OrderStatus
    {
        Pending,
        Paid,
        Cancelled,
        Refunded
    }
    public class Order
    {
        [Key]
        public int OrderId { get; set; }
        public int? UserId { get; set; }
        [Required]
        [Column(TypeName = "decimal(10,2)")] // Matched to SQL DECIMAL(10,2)
        public decimal TotalAmount { get; set; }
        [Required]
        public OrderStatus Status { get; set; } = OrderStatus.Pending;
        public TripType TripType { get; set; } = TripType.OneWay;
        [StringLength(50)]
        public string? PaymentMethod { get; set; } = string.Empty;
        public int? PromotionId { get; set; }
        [StringLength(100)]
        [EmailAddress]
        public string? GuestEmail { get; set; } = string.Empty;
        [StringLength(10)]
        [Phone]
        public string? GuestPhone { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public virtual User? User { get; set; }
        public virtual Promotion? Promotion { get; set; }
        [NotMapped]
        public string StatusVietnamese
        {
            get
            {
                switch (this.Status)
                {
                    case OrderStatus.Pending:
                        return "Chờ thanh toán";
                    case OrderStatus.Paid:
                        return "Đã thanh toán";
                    case OrderStatus.Cancelled:
                        return "Đã hủy";
                    case OrderStatus.Refunded:
                        return "Đã hoàn tiền";
                    default:
                        return this.Status.ToString(); // Hoặc "Không xác định"
                }
            }
        }
        [NotMapped]
        public int PaidTicketCount{ get; set; }
        public virtual ICollection<OrderTicket> OrderTickets { get; set; } = new List<OrderTicket>();
        public virtual ICollection<Cancellation> Cancellations { get; set; } = new List<Cancellation>(); // Already here, good.
        public virtual ICollection<Payment> Payments{ get; set; }= new List<Payment>();
        public virtual ICollection<Ticket> Tickets { get; set; } = new List<Ticket>(); // Add if Order directly relates to Tickets
    
    }
}