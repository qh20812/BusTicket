using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BusTicketSystem.Pages;

[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
[IgnoreAntiforgeryToken]
public class ErrorModel : PageModel
{
    public string? RequestId { get; set; }
    public string PageTitle { get; set; } = "Lỗi";
    public string ErrorMessage { get; set; } = "Đã có lỗi xảy ra trong quá trình xử lý yêu cầu của bạn.";
    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

    private readonly ILogger<ErrorModel> _logger;

    public ErrorModel(ILogger<ErrorModel> logger)
    {
        _logger = logger;
    }

    public void OnGet(string? errorCode = null)
    {
        RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        if (errorCode == "AccessDenied")
        {
            PageTitle = "Từ chối truy cập";
            ErrorMessage = "Bạn không có quyền truy cập vào trang này do vai trò không phù hợp.";
        }
        ViewData["Title"] = PageTitle;
    }
}
