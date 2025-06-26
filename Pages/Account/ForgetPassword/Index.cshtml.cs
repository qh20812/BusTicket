// using System.ComponentModel.DataAnnotations;
// using System.Security.Cryptography;
// using BusTicketSystem.Data;
// using Microsoft.AspNetCore.Mvc;
// using Microsoft.AspNetCore.Mvc.RazorPages;
// using Microsoft.EntityFrameworkCore;
// using BusTicketSystem.Services;

// namespace BusTicketSystem.Pages.Account.ForgetPassword
// {
//     public class ForgetPasswordModel : PageModel
//     {
//         private readonly AppDbContext _context;
//         private readonly IEmailSender _emailSender;
//         public ForgetPasswordModel(AppDbContext context, IEmailSender emailSender)
//         {
//             _context = context;
//             _emailSender = emailSender;
//         }
//         [BindProperty]
//         [Required(ErrorMessage = "Vui lòng nhập địa chỉ email của bạn.")]
//         [EmailAddress(ErrorMessage = "Địa chỉ email không hợp lệ.")]
//         public string Email { get; set; }
//         [TempData]
//         public string? StatusMessage { get; set; }
//         public void OnGet()
//         {

//         }
//         public async Task<IActionResult> OnPostAsync()
//         {
//             if (!ModelState.IsValid)
//             {
//                 return Page();
//             }
//             var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == Email && u.IsActive);
//             if (user != null)
//             {
//                 var tokenBytes = RandomNumberGenerator.GetBytes(32);
//                 var resetToken = Convert.ToBase64String(tokenBytes)
//                     .Replace('+', ',').Replace('/', '_').TrimEnd('=');
//                 user.PasswordResetToken = resetToken;
//                 user.PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(1);
//                 user.UpdatedAt = DateTime.UtcNow;

//                 _context.Users.Update(user);
//                 await _context.SaveChangesAsync();

//                 var resetLink = Url.Page(
//                     "/Account/ResetPassword/ResetPassword",
//                     pageHandler: null,
//                     values: new { email = user.Email, token = resetToken },
//                     protocol: Request.Scheme
//                 );
//                 await _emailSender.SendPasswordResetEmailAsync(user.Email, "Đặt lại mật khẩu", $"Vui lòng đặt lại mật khẩu bằng cách nhấp vào liên kết này: {resetLink}");
//                 // System.Diagnostics.Debug.WriteLine($"Password Reset Link for {user.Email}: {resetLink}");
//                 StatusMessage = "Nếu địa chỉ email của bạn tồn tại trong hệ thống và tài khoản đang hoạt động, chúng tôi đã gửi một liên kết để đặt lại mật khẩu của bạn. Vui lòng kiểm tra hộp thư đến (và thư mục spam)";
//             }
//             else
//             {
//                 StatusMessage = "Nếu địa chỉ email của bạn tồn tại trong hệ thống và tài khoản đang hoạt động, chúng tôi đã gửi một liên kết để đặt lại mật khẩu của bạn. Vui lòng kiểm tra hộp thư đến (và thư mục spam)";
//             }
//             return RedirectToPage();
//         }
//     }
// }