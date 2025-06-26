using System.ComponentModel.DataAnnotations;

namespace BusTicketSystem.Models
{
    public class LoginHistory
    {
        [Key]
        public int Id { get; set; }
        public int? UserId { get; set; }
        [StringLength(50)]
        public string? Action { get; set; } // Changed to nullable to match DB
        [StringLength(45)]
        public string? IpAddress { get; set; } // Added
        public string? UserAgent { get; set; } // Added (TEXT in DB)
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public User? User{ get; set; }
    }
}