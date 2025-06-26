using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BusTicketSystem.Models
{
    public enum DiscountType {
        Percentage,
        Fixed
    }
    public enum PromotionStatus
    {
        Active,
        Inactive,
        Expired
    }
    public class Promotion
    {
        [Key]
        public int PromotionId { get; set; }
        [Required]
        [StringLength(20)]
        public string Code { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        [Required]
        public DiscountType DiscountType { get; set; }
        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal DiscountValue { get; set; }
        [Column(TypeName = "decimal(10,2)")]
        public decimal MinOrderAmount { get; set; } = 0;
        public int? RouteId { get; set; }
        [Required]
        public DateTime StartDate { get; set; }
        [Required]
        public DateTime EndDate { get; set; }
        public int? MaxUsage { get; set; }
        public int UsedCount { get; set; } = 0;
        public bool IsFirstOrder { get; set; } = false; // Matches BOOLEAN DEFAULT FALSE
        [Required]
        public PromotionStatus Status { get; set; } = PromotionStatus.Active;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        //relationship
        public Route? Route { get; set; }
        public required ICollection<Order> Orders{ get; set; }
    }
}