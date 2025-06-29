using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BusTicketSystem.Pages.ForAdmin.SearchHandler
{
    public class IndexModel : PageModel
    {
        public IActionResult OnGet(string searchTerm)
        {
            if (string.IsNullOrEmpty(searchTerm))
            {
                return RedirectToPage("/ForAdmin/Dashboard/Index");
            }
            if (searchTerm.StartsWith("ORD"))
            {
                return RedirectToPage("/ForAdmin/OrderManage/Index", new { searchTerm = searchTerm });
            }
            else if (searchTerm.StartsWith("KH"))
            {
                return RedirectToPage("/ForAdmin/CustomerManage/Index", new { searchTerm = searchTerm });
            }
            return RedirectToPage("/ForAdmin/OrderManage/Index", new { searchTerm = searchTerm });
        }
    }
}