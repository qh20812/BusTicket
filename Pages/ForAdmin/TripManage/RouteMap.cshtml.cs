using BusTicketSystem.Data;
using BusTicketSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BusTicketSystem.Pages.ForAdmin.TripManage
{
    public class RouteMapModel : PageModel
    {
        private readonly AppDbContext _context;

        public RouteMapModel(AppDbContext context)
        {
            _context = context;
        }

        public Trip? CurrentTrip { get; set; }
        // public List<TripStop> RouteStations { get; set; } = new(); // Sẽ thay thế bằng AllMapPoints
        public List<MapPointViewModel> AllMapPoints { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int? tripId)
        {
            if (tripId == null)
            {
                return NotFound();
            }

            CurrentTrip = await _context.Trips
                .Include(t => t.Route) // Include Route for breadcrumbs or title
                .Include(t => t.TripStops) // Không cần OrderBy ở đây nữa, sẽ sắp xếp sau khi gộp
                .FirstOrDefaultAsync(t => t.TripId == tripId);

            if (CurrentTrip == null)
            {
                return NotFound();
            }

            // Tạo danh sách các điểm để hiển thị trên bản đồ
            var points = new List<MapPointViewModel>();
            int currentOrder = 1;

            // 1. Thêm điểm đi
            if (CurrentTrip.Route != null && !string.IsNullOrEmpty(CurrentTrip.Route.OriginCoordinates))
            {
                var originCoordsStr = CurrentTrip.Route.OriginCoordinates.Split(',');
                if (originCoordsStr.Length == 2 && 
                    double.TryParse(originCoordsStr[0], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double originLat) &&
                    double.TryParse(originCoordsStr[1], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double originLon))
                {
                    points.Add(new MapPointViewModel
                    {
                        StationName = $"Điểm đi: {CurrentTrip.Route.Departure}",
                        Latitude = originLat,
                        Longitude = originLon,
                        StopOrder = currentOrder++,
                        PointType = "origin"
                    });
                }
            }

            // 2. Thêm các điểm dừng (TripStops)
            if (CurrentTrip.TripStops != null)
            {
                foreach (var stop in CurrentTrip.TripStops.OrderBy(ts => ts.StopOrder))
                {
                    points.Add(new MapPointViewModel
                    {
                        StationName = stop.StationName,
                        Latitude = stop.Latitude,
                        Longitude = stop.Longitude,
                        StopOrder = currentOrder++, // Gán thứ tự dựa trên thứ tự đã có hoặc tăng dần
                        EstimatedArrival = stop.EstimatedArrival,
                        EstimatedDeparture = stop.EstimatedDeparture,
                        Note = stop.Note,
                        PointType = "stop"
                    });
                }
            }

            // 3. Thêm điểm đến
            if (CurrentTrip.Route != null && !string.IsNullOrEmpty(CurrentTrip.Route.DestinationCoordinates))
            {
                var destCoordsStr = CurrentTrip.Route.DestinationCoordinates.Split(',');
                if (destCoordsStr.Length == 2 &&
                    double.TryParse(destCoordsStr[0], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double destLat) &&
                    double.TryParse(destCoordsStr[1], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double destLon))
                {
                    points.Add(new MapPointViewModel
                    {
                        StationName = $"Điểm đến: {CurrentTrip.Route.Destination}",
                        Latitude = destLat,
                        Longitude = destLon,
                        StopOrder = currentOrder++,
                        PointType = "destination"
                    });
                }
            }

            AllMapPoints = points.OrderBy(p => p.StopOrder).ToList();

            return Page();
        }
    }

    // ViewModel mới để biểu diễn một điểm trên bản đồ
    public class MapPointViewModel : TripStop // Kế thừa TripStop để có các thuộc tính sẵn có, hoặc tạo mới hoàn toàn
    {
        public string PointType { get; set; } = "stop"; // "origin", "stop", "destination"
    }
}