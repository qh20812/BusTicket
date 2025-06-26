using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BusTicketSystem.Models
{
    public class Menu
    {
        [Key]
        public int MenuId { get; set; }
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;
        [Required]
        [StringLength(255)]
        public string Url { get; set; } = string.Empty;
        public int? ParentId { get; set; }
        public Menu? ParentMenu { get; set; }
        public ICollection<Menu> ChildMenus { get; set; } = new List<Menu>();
        public int DisplayOrder { get; set; } = 0;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}