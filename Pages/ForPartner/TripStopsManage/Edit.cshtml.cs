using BusTicketSystem.Data;
using BusTicketSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;

namespace BusTicketSystem.Pages.ForPartner.TripStopsManage
{
    [Authorize(Roles = "Partner")]
    public class TripStopEditModel : PageModel // Đổi tên class thành TripStopEditModel
    {
        private readonly AppDbContext _context;

        public TripStopEditModel(AppDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public TripStopInputModel StopInput { get; set; } = new TripStopInputModel();

        public string PartnerCompanyName { get; set; } = string.Empty;

        private int GetCurrentPartnerBusCompanyId()
        {
            var busCompanyIdClaim = User.FindFirstValue("CompanyId");
            if (int.TryParse(busCompanyIdClaim, out int busCompanyId))
            {
                return busCompanyId;
            }
            throw new Exception("BusCompanyId not found for the current partner.");
        }

        private async Task LoadPartnerCompanyName(int companyId)
        {
            var company = await _context.BusCompanies.FindAsync(companyId);
            PartnerCompanyName = company?.CompanyName ?? "Không xác định";
        }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            int partnerBusCompanyId = GetCurrentPartnerBusCompanyId();
            await LoadPartnerCompanyName(partnerBusCompanyId);

            if (id == null)
            {
                ViewData["Title"] = "Thêm mới điểm dừng";
                StopInput = new TripStopInputModel();
                return Page();
            }

            ViewData["Title"] = "Chỉnh sửa điểm dừng";
            var stop = await _context.TripStops
                .FirstOrDefaultAsync(s => s.StopId == id && s.Trip.CompanyId == partnerBusCompanyId);

            if (stop == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy điểm dừng hoặc bạn không có quyền chỉnh sửa.";
                return RedirectToPage("./Index");
            }

            StopInput = new TripStopInputModel
            {
                StopId = stop.StopId,
                StationName = stop.StationName,
                Latitude = stop.Latitude,
                Longitude = stop.Longitude,
                Note = stop.Note
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            int partnerBusCompanyId = GetCurrentPartnerBusCompanyId();
            await LoadPartnerCompanyName(partnerBusCompanyId);

            if (!ModelState.IsValid)
            {
                ViewData["Title"] = StopInput.StopId == 0 ? "Thêm mới điểm dừng" : "Chỉnh sửa điểm dừng";
                return Page();
            }

            if (StopInput.StopId == 0)
            {
                var newStop = new TripStop
                {
                    StationName = StopInput.StationName,
                    Latitude = StopInput.Latitude,
                    Longitude = StopInput.Longitude,
                    Note = StopInput.Note
                };
                _context.TripStops.Add(newStop);
                TempData["SuccessMessage"] = "Đã thêm mới điểm dừng.";
            }
            else
            {
                var stopToUpdate = await _context.TripStops
                    .FirstOrDefaultAsync(s => s.StopId == StopInput.StopId && s.Trip.CompanyId == partnerBusCompanyId);

                if (stopToUpdate == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy điểm dừng hoặc bạn không có quyền chỉnh sửa.";
                    return RedirectToPage("./Index");
                }

                stopToUpdate.StationName = StopInput.StationName;
                stopToUpdate.Latitude = StopInput.Latitude;
                stopToUpdate.Longitude = StopInput.Longitude;
                stopToUpdate.Note = StopInput.Note;

                _context.Attach(stopToUpdate).State = EntityState.Modified;
                TempData["SuccessMessage"] = "Đã cập nhật điểm dừng.";
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.TripStops.Any(s => s.StopId == StopInput.StopId))
                {
                    return NotFound();
                }
                ModelState.AddModelError(string.Empty, "Có lỗi xảy ra khi cập nhật dữ liệu. Vui lòng thử lại.");
                return Page();
            }

            return RedirectToPage("./Index");
        }
    }

    public class TripStopInputModel
    {
        public int StopId { get; set; }

        [Required(ErrorMessage = "Tên trạm không được để trống.")]
        [StringLength(255)]
        [RegularExpression(@"^[\p{L}\p{N}\s-]+$", ErrorMessage = "Tên trạm chỉ được chứa chữ cái, số, khoảng trắng và dấu gạch ngang.")]
        [Display(Name = "Tên trạm")]
        public string StationName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vĩ độ không được để trống.")]
        [Display(Name = "Vĩ độ")]
        [Range(-90.0, 90.0, ErrorMessage = "Vĩ độ không hợp lệ.")]
        public double Latitude { get; set; }

        [Required(ErrorMessage = "Kinh độ không được để trống.")]
        [Display(Name = "Kinh độ")]
        [Range(-180.0, 180.0, ErrorMessage = "Kinh độ không hợp lệ.")]
        public double Longitude { get; set; }

        [StringLength(500)]
        [Display(Name = "Ghi chú")]
        public string? Note { get; set; }
    }
}