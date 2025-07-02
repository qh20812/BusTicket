using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BusTicketSystem.Models;

namespace BusTicketSystem.Models
{
    public enum TicketStatus
    {
        Booked,
        Cancelled,
        Used
    }

    public enum TicketType
    {
        OneWay,
        RoundTrip
    }
    public class Ticket
    {
        [Key]
        public int TicketId { get; set; }
        [Required]
        public int TripId { get; set; }
        public int? UserId { get; set; }
        [Required] // Một vé phải thuộc về một đơn hàng
        public int OrderId { get; set; } // Thêm thuộc tính này

        [Required]
        [StringLength(10)]
        public string SeatNumber { get; set; } = string.Empty;
        [Required] // Matches SQL DECIMAL(10,2)
        [Column(TypeName = "decimal(10,2)")]
        public decimal Price { get; set; }
        
        [Required]
        public TicketType Type { get; set; } = TicketType.OneWay;

        [Required]
        public TicketStatus Status { get; set; } = TicketStatus.Booked;
        public DateTime BookedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CancelledAt { get; set; }
        //relationship
        public Trip? Trip { get; set; }
        public Order? Order { get; set; } // Thêm thuộc tính navigation này
        public User? User { get; set; }
        public  ICollection<OrderTicket> OrderTickets { get; set; }= new List<OrderTicket>();

        public  ICollection<Cancellation> Cancellations{ get; set; }= new List<Cancellation>();
    
    }
}