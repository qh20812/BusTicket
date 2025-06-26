using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BusTicketSystem.Helpers;

namespace BusTicketSystem.Pages.Account.Logout
{
    public class LogoutModel : PageModel
    {
        // Xử lý GET request: có thể redirect hoặc thực hiện logout.
        // Để nhất quán và tránh lỗi nếu người dùng bookmark/gõ URL,
        // OnGet cũng có thể thực hiện logout.
        public IActionResult OnGet()
        {
            HttpContext.Session.ClearUser();
            return RedirectToPage("/Index");
        }

        // Xử lý POST request từ form
        public IActionResult OnPost()
        {
            HttpContext.Session.ClearUser();
            return RedirectToPage("/Index");
        }
    }
}