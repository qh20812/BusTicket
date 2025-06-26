using System.ComponentModel.DataAnnotations;

namespace BusTicketSystem.Models
{
    public class OrderTicket
    {
        [Key]
        public int OrderTicketId { get; set; }
        [Required]
        public int OrderId { get; set; }
        [Required]
        public int TicketId { get; set; }
        public Order? Order { get; set; }
        public Ticket? Ticket{ get; set; }
    }
}