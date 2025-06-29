using BusTicketSystem.Data;
using BusTicketSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace BusTicketSystem.Pages.ForAdmin.CustomerManage
{
    [Authorize(Roles = "Admin")]
    public class EditModel : PageModel
    {
        private readonly AppDbContext _context;

        public EditModel(AppDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public CustomerInputModel CustomerInput { get; set; } = new CustomerInputModel();

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                TempData["ErrorMessage"] = "ID khách hàng không hợp lệ.";
                return RedirectToPage("./Index");
            }

            var customer = await _context.Users.AsNoTracking().FirstOrDefaultAsync(m => m.UserId == id);

            if (customer == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy khách hàng.";
                return RedirectToPage("./Index");
            }

#pragma warning disable CS8601 // Possible null reference assignment.
            CustomerInput = new CustomerInputModel
            {
                UserId = customer.UserId,
                Fullname = customer.Fullname,
                Email = customer.Email,
                Username = customer.Username,
                Phone = customer.Phone,
                Address = customer.Address,
                Role = customer.Role,
                PasswordHash = customer.PasswordHash,
                CreatedAt = customer.CreatedAt,
                IsActive = customer.IsActive // Populate IsActive
            };
#pragma warning restore CS8601 // Possible null reference assignment.

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var customerToUpdate = await _context.Users.FindAsync(CustomerInput.UserId);

            if (customerToUpdate == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy khách hàng để cập nhật.";
                return RedirectToPage("./Index");
            }

            // Removed uniqueness checks for Username and Email as they are now read-only.

            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Only Role is editable. Other important information is preserved.
            // customerToUpdate.Fullname = CustomerInput.Fullname; // Read-only
            // customerToUpdate.Email = CustomerInput.Email;       // Read-only
            // customerToUpdate.Username = CustomerInput.Username; // Read-only
            // customerToUpdate.Phone = CustomerInput.Phone;       // Read-only
            // customerToUpdate.Address = CustomerInput.Address;   // Read-only

            // Update Role
            customerToUpdate.Role = CustomerInput.Role;

            // Preserve non-editable fields that were loaded and passed via hidden fields
            customerToUpdate.PasswordHash = CustomerInput.PasswordHash;
            customerToUpdate.CreatedAt = CustomerInput.CreatedAt;
            customerToUpdate.IsActive = CustomerInput.IsActive; // Preserve IsActive status

            customerToUpdate.UpdatedAt = DateTime.UtcNow;

            try
            {
                await _context.SaveChangesAsync();
                var notificationMessage = $"Thông tin khách hàng '{customerToUpdate.Fullname}' (ID: {customerToUpdate.UserId}) đã được cập nhật.";
                var newNotification = new Notification
                {
                    Message = notificationMessage,
                    Category = NotificationCategory.Customer,
                    TargetUrl = Url.Page("/ForAdmin/CustomerManage/Edit", new { id = customerToUpdate.UserId }),
                    IconCssClass = "bi bi-person-check-fill",
                    RecipientUserId = null // General admin notification
                };
                _context.Notifications.Add(newNotification);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Thông tin khách hàng '{customerToUpdate.Fullname}' đã được cập nhật thành công.";
                return RedirectToPage("./Index");
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Users.Any(e => e.UserId == CustomerInput.UserId))
                {
                    TempData["ErrorMessage"] = "Lỗi tương tranh: Khách hàng không còn tồn tại hoặc đã bị xóa.";
                    return RedirectToPage("./Index");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Dữ liệu đã được người khác thay đổi. Vui lòng tải lại trang và thử lại.");
                }
            }
            catch (DbUpdateException ex)
            {
                ModelState.AddModelError(string.Empty, "Đã xảy ra lỗi khi lưu vào cơ sở dữ liệu. Vui lòng thử lại. " + ex.Message);
            }

            return Page();
        }
    }
}