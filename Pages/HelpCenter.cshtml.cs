using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BusTicketSystem.Pages;

public class HelpCenterModel : PageModel
{
    private readonly ILogger<HelpCenterModel> _logger;
    public HelpCenterModel(ILogger<HelpCenterModel> logger)
    {
        _logger = logger;
    }
    public void OnGet()
    {
    }
}