using BusTicketSystem.Data;
using BusTicketSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace BusTicketSystem.Pages.ForAdmin.TripStopsManage
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
        public StopInputModel StopInput { get; set; } = new StopInputModel();

        public string PartnerCompanyName { get; set; } = string.Empty;

        private async Task LoadPartnerCompanyName(int? companyId)
        {
            if (companyId.HasValue)
            {
                var company = await _context.BusCompanies.FindAsync(companyId.Value);
                PartnerCompanyName = company?.CompanyName ?? "Không xác định";
            }
            else
            {
                PartnerCompanyName = "Hệ thống";
            }
        }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var stop = await _context.Stops.FindAsync(id);
            if (stop == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy trạm dừng.";
                return RedirectToPage("./Index");
            }

            await LoadPartnerCompanyName(stop.CompanyId);

            StopInput = new StopInputModel
            {
                StopId = stop.StopId,
                StopName = stop.StopName,
                Latitude = stop.Latitude,
                Longitude = stop.Longitude,
                Address = stop.Address,
                CompanyId = stop.CompanyId,
                Status = stop.Status
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(bool UseForAll, string? RejectionReason)
        {
            if (!ModelState.IsValid)
            {
                await LoadPartnerCompanyName(StopInput.CompanyId);
                return Page();
            }

            var stop = await _context.Stops.FindAsync(StopInput.StopId);
            if (stop == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy trạm dừng.";
                return RedirectToPage("./Index");
            }

            stop.Status = StopInput.Status!.Value;
            if (UseForAll)
            {
                stop.CompanyId = null;
            }
            stop.UpdatedAt = DateTime.UtcNow;

            if (stop.Status == StopStatus.Rejected)
            {
                // Gửi thông báo từ chối
                var notification = new Notification
                {
                    RecipientUserId = null, // Gửi đến Partner liên quan
                    Message = $"Trạm dừng {stop.StopName} đã bị từ chối. Lý do: {RejectionReason}",
                    Category = NotificationCategory.System,
                    TargetUrl = $"/ForPartner/TripStopsManage/Edit?id={stop.StopId}",
                    CreatedAt = DateTime.UtcNow
                };
                _context.Notifications.Add(notification);
            }
            else if (stop.Status == StopStatus.Approved)
            {
                // Gửi thông báo duyệt
                var notification = new Notification
                {
                    RecipientUserId = null,
                    Message = $"Trạm dừng {stop.StopName} đã được duyệt và {(UseForAll ? "dùng chung cho hệ thống" : "chỉ dùng cho nhà xe của bạn")}.",
                    Category = NotificationCategory.System,
                    TargetUrl = $"/ForPartner/TripStopsManage/Edit?id={stop.StopId}",
                    CreatedAt = DateTime.UtcNow
                };
                _context.Notifications.Add(notification);
            }

            _context.Attach(stop).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã cập nhật trạng thái trạm dừng '{stop.StopName}' thành công.";
            return RedirectToPage("./Index");
        }
    }

    public class StopInputModel
    {
        public int StopId { get; set; }

        [Required(ErrorMessage = "Tên trạm dừng không được để trống.")]
        [StringLength(255, ErrorMessage = "Tên trạm dừng không được vượt quá 255 ký tự.")]
        [Display(Name = "Tên trạm dừng")]
        public string StopName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vĩ độ không được để trống.")]
        [Range(-90.0, 90.0, ErrorMessage = "Vĩ độ không hợp lệ.")]
        [Display(Name = "Vĩ độ")]
        public double Latitude { get; set; }

        [Required(ErrorMessage = "Kinh độ không được để trống.")]
        [Range(-180.0, 180.0, ErrorMessage = "Kinh độ không hợp lệ.")]
        [Display(Name = "Kinh độ")]
        public double Longitude { get; set; }

        [StringLength(500, ErrorMessage = "Địa chỉ không được vượt quá 500 ký tự.")]
        [Display(Name = "Địa chỉ")]
        public string? Address { get; set; }

        [Display(Name = "Công ty")]
        public int? CompanyId { get; set; }

        [Required(ErrorMessage = "Trạng thái không được để trống.")]
        [Display(Name = "Trạng thái")]
        public StopStatus? Status { get; set; }
    }
}