using BusTicketSystem.Data;
using BusTicketSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using System.ComponentModel.DataAnnotations;

namespace BusTicketSystem.Pages.ForAdmin.TripStopsManage
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;

        public IndexModel(AppDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public StopInputModel StopInput { get; set; } = new StopInputModel();

        public IList<Stop> Stops { get; set; } = new List<Stop>();

        [BindProperty(SupportsGet = true)]
        public string SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public string SortOrder { get; set; }

        public async Task OnGetAsync()
        {
            var query = _context.Stops.AsNoTracking();

            // Tìm kiếm
            if (!string.IsNullOrEmpty(SearchTerm))
            {
                query = query.Where(s => s.StopName.Contains(SearchTerm) || (s.Address != null && s.Address.Contains(SearchTerm)));
            }

            // Sắp xếp
            query = SortOrder switch
            {
                "name_desc" => query.OrderByDescending(s => s.StopName),
                "created_asc" => query.OrderBy(s => s.CreatedAt),
                "created_desc" => query.OrderByDescending(s => s.CreatedAt),
                _ => query.OrderBy(s => s.StopName) // Mặc định: name_asc
            };

            Stops = await query.ToListAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                // Tải lại danh sách với bộ lọc và sắp xếp hiện tại để đảm bảo tính nhất quán
                await OnGetAsync();
                return Page();
            }

            string successMessage;

            if (StopInput.StopId == 0)
            {
                var newStop = new Stop
                {
                    StopName = StopInput.StopName,
                    Latitude = StopInput.Latitude,
                    Longitude = StopInput.Longitude,
                    Address = StopInput.Address,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.Stops.Add(newStop);
                successMessage = "Đã thêm mới trạm dừng thành công.";
            }
            else
            {
                var stopToUpdate = await _context.Stops.FindAsync(StopInput.StopId);
                if (stopToUpdate == null)
                {
                    return NotFound("Không tìm thấy trạm dừng.");
                }
                stopToUpdate.StopName = StopInput.StopName;
                stopToUpdate.Latitude = StopInput.Latitude;
                stopToUpdate.Longitude = StopInput.Longitude;
                stopToUpdate.Address = StopInput.Address;
                stopToUpdate.UpdatedAt = DateTime.UtcNow;
                _context.Attach(stopToUpdate).State = EntityState.Modified;
                successMessage = "Đã cập nhật trạm dừng thành công.";
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = successMessage;
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int stopId)
        {
            var stop = await _context.Stops.FindAsync(stopId);
            if (stop == null)
            {
                return NotFound("Không tìm thấy trạm dừng.");
            }

            // Kiểm tra xem trạm dừng có đang được sử dụng trong TripStop không
            var isUsed = await _context.TripStops.AnyAsync(ts => ts.StationName == stop.StopName);
            if (isUsed)
            {
                return BadRequest("Không thể xóa trạm dừng này vì nó đang được sử dụng trong một số chuyến đi.");
            }

            _context.Stops.Remove(stop);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Đã xóa trạm dừng thành công.";
            return new JsonResult(new { success = true });
        }
    }
}