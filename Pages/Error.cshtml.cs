using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Diagnostics;

namespace BusTicketSystem.Pages
{
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [IgnoreAntiforgeryToken]
    public class ErrorModel : PageModel
    {
        public string? RequestId { get; set; }
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

        public int? StatusCode { get; set; }
        public string PageTitle { get; set; } = "Lỗi";
        public string ErrorMessage { get; set; } = "Đã có lỗi xảy ra.";

        public void OnGet(int? statusCode = null)
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
            StatusCode = statusCode;

            switch (StatusCode)
            {
                case 403:
                    PageTitle = "Truy Cập Bị Từ Chối";
                    ErrorMessage = "Bạn không có quyền truy cập vào trang này.";
                    break;
                case 404:
                    PageTitle = "Không Tìm Thấy Trang";
                    ErrorMessage = "Trang bạn đang tìm kiếm không tồn tại.";
                    break;
                default:
                    // Dành cho UseExceptionHandler, khi đó statusCode sẽ là null.
                    // Mặc định là lỗi 500 cho các lỗi server.
                    if (!StatusCode.HasValue) {
                        StatusCode = 500;
                    }
                    PageTitle = "Đã Có Lỗi Xảy Ra";
                    ErrorMessage = "Một lỗi không mong muốn đã xảy ra khi xử lý yêu cầu của bạn.";
                    break;
            }
        }
    }
}

