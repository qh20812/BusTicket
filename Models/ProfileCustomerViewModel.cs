using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace BusTicketSystem.Models
{
    public class ProfileCustomerViewModel
    {
        public int UserId { get; set; } // Hidden field, useful for updates

        [Display(Name = "Tên đăng nhập")]
        public string Username { get; set; } = string.Empty; // Usually not editable, but display

        [Required(ErrorMessage = "Email không được để trống.")]
        [EmailAddress(ErrorMessage = "Địa chỉ email không hợp lệ.")]
        [StringLength(100)]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [StringLength(100)]
        [Display(Name = "Họ và tên")]
        public string? Fullname { get; set; }

        [StringLength(20, ErrorMessage = "Số điện thoại không hợp lệ.")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
        [Display(Name = "Số điện thoại")]
        public string? Phone { get; set; }

        [Display(Name = "Địa chỉ")]
        public string? Address { get; set; }

        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu mới phải có ít nhất 6 ký tự.")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu mới (để trống nếu không đổi)")]
        public string? NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Xác nhận mật khẩu mới")]
        [Compare("NewPassword", ErrorMessage = "Mật khẩu mới và mật khẩu xác nhận không khớp.")]
        public string? ConfirmNewPassword { get; set; }
        [Display(Name = "Ảnh đại diện mới")]
        public IFormFile? AvatarFile { get; set; }
        public string? CurrentAvatarPath{ get; set; }
    }
}