using BusTicketSystem.Data;
using BusTicketSystem.Models; // Giả sử User model nằm ở đây
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace BusTicketSystem.Pages.ForAdmin.CustomerManage // Sửa namespace cho đúng
{
    [Authorize(Roles ="Admin")]
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;

        public IndexModel(AppDbContext context)
        {
            _context = context;
        }

        public IList<User> Customers { get; set; } = new List<User>();

        [BindProperty(SupportsGet = true)]
        public string? SearchName { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? SortName { get; set; }

        public async Task OnGetAsync()
        {
            IQueryable<User> query = _context.Users.Where(u => u.Role == "Member");

            if (!string.IsNullOrEmpty(SearchName))
            {
                query = query.Where(u => u.Fullname != null && u.Fullname.Contains(SearchName));
            }
            switch (SortName?.ToLower())
            {
                case "az":
                    query = query.OrderBy(u => u.Fullname);
                    break;
                case "za":
                    query = query.OrderByDescending(u => u.Fullname);
                    break;
                default:
                    query = query.OrderByDescending(u => u.CreatedAt);
                    break;
            }

            Customers = await query.ToListAsync();
        }

        public async Task<IActionResult> OnPostBlockAsync(int id)
        {
            var customer = await _context.Users.FindAsync(id);

            if (customer == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy khách hàng.";
                return RedirectToPage("./Index");
            }

            // Assuming User model has IsActive property and Notification model/category exists
            if (customer.IsActive)
            {
                customer.IsActive = false;
                customer.UpdatedAt = DateTime.UtcNow; // Update timestamp

                var notificationMessage = $"Khách hàng '{customer.Fullname}' (ID: {customer.UserId}) đã bị chặn.";
                var newNotification = new Notification
                {
                    Message = notificationMessage,
                    Category = NotificationCategory.Customer,
                    TargetUrl = Url.Page("/ForAdmin/CustomerManage/Edit", new { id = customer.UserId }),
                    IconCssClass = "bi bi-person-slash-fill",
                    RecipientUserId = null // General admin notification
                };
                _context.Notifications.Add(newNotification);

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Khách hàng '{customer.Fullname}' đã được chặn thành công.";
            }
            else
            {
                TempData["InfoMessage"] = $"Khách hàng '{customer.Fullname}' đã bị chặn trước đó.";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostUnblockAsync(int id)
        {
            var customer = await _context.Users.FindAsync(id);

            if (customer == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy khách hàng.";
                return RedirectToPage("./Index");
            }

            if (!customer.IsActive)
            {
                customer.IsActive = true;
                customer.UpdatedAt = DateTime.UtcNow; // Update timestamp

                var notificationMessage = $"Khách hàng '{customer.Fullname}' (ID: {customer.UserId}) đã được mở chặn.";
                var newNotification = new Notification
                {
                    Message = notificationMessage,
                    Category = NotificationCategory.Customer,
                    TargetUrl = Url.Page("/ForAdmin/CustomerManage/Edit", new { id = customer.UserId }),
                    IconCssClass = "bi bi-person-check-fill",
                    RecipientUserId = null // General admin notification
                };
                _context.Notifications.Add(newNotification);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Khách hàng '{customer.Fullname}' đã được mở chặn thành công.";
            }
            else
            {
                TempData["InfoMessage"] = $"Khách hàng '{customer.Fullname}' đã được kích hoạt trước đó.";
            }
            return RedirectToPage();
        }

    }
}