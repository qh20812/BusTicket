using System.ComponentModel.DataAnnotations;

namespace BusTicketSystem.Models
{
    public enum PostCategory
    {
        [Display(Name ="Tin tức")]
        News,
        // [Display(Name ="Khuyến mãi")]
        // Promotion,
        [Display(Name ="Thông báo")]
        Announcement,
        [Display(Name ="Hướng dẫn")]
        Guide
    }

    public enum PostStatus
    {
        [Display(Name ="Bản nháp")]
        Draft,
        [Display(Name ="Đã xuất bản")]
        Published,
        [Display(Name ="Đã lưu trữ")]
        Archived
    }
    public class Post
    {
        [Key]
        public int PostId { get; set; }
        [Required]
        public int UserId { get; set; }
        [Required]
        [StringLength(255)]
        public string Title { get; set; } = string.Empty;
        [Required]
        public string Content { get; set; } = string.Empty;
        [Required]
        public PostCategory Category { get; set; }
        [Required]
        public PostStatus Status { get; set; } = PostStatus.Draft;
        
        // Thêm thuộc tính mới cho URL hình ảnh
        [StringLength(1024)] // Giới hạn độ dài URL nếu cần
        public string? ImageUrl { get; set; } // Cho phép null nếu hình ảnh không bắt buộc

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        public User? User{ get; set; }
    }
}