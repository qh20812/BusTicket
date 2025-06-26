using BusTicketSystem.Data;
using BusTicketSystem.Models;
using BusTicketSystem.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace BusTicketSystem.Pages.Home.Schedule
{
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;

        public IndexModel(AppDbContext context)
        {
            _context = context;
        }

        // Tham số tìm kiếm từ trang chủ
        [BindProperty(SupportsGet = true)]
        public string? Departure { get; set; } // Điểm đi
        [BindProperty(SupportsGet = true)]
        public string? Destination { get; set; } // Điểm đến
        [BindProperty(SupportsGet = true, Name = "date")] // Khớp với tên trong form Index.cshtml
        [DataType(DataType.Date)]
        public DateTime? DepartureDate { get; set; } // Ngày khởi hành
        [BindProperty(SupportsGet = true, Name = "returnDateStr")] // Khớp với tên trong form Index.cshtml
        [DataType(DataType.Date)]
        public DateTime? ReturnDate { get; set; } // Ngày về (cho khứ hồi, không dùng trong logic hiển thị này)
        [BindProperty(SupportsGet = true)]
        [Range(1, 10, ErrorMessage = "Số lượng vé không hợp lệ.")] // Xác thực cơ bản
        public int Quantity { get; set; } = 1; // Số lượng vé
        [BindProperty(SupportsGet = true)]
        public string? TripType { get; set; } // Loại chuyến đi: "one-way" hoặc "round-trip"

        // Tham số lọc và sắp xếp
        [BindProperty(SupportsGet = true)]
        public string? SortOrder { get; set; } // Ví dụ: "price_asc", "departure_asc"
        [BindProperty(SupportsGet = true)]
        public decimal? MinPrice { get; set; } // Giá tối thiểu
        [BindProperty(SupportsGet = true)]
        public decimal? MaxPrice { get; set; } // Giá tối đa
        // Thêm các thuộc tính lọc khác nếu cần (ví dụ: khoảng thời gian, hãng xe)

        public IList<TripViewModel> AvailableTrips { get; set; } = new List<TripViewModel>(); // Danh sách chuyến đi khả dụng
        public IList<string> AvailableDepartures { get; set; } = new List<string>(); // Danh sách các điểm đi
        public IList<TripViewModel> AvailableReturnTrips { get; set; } = new List<TripViewModel>();
        public IList<string> AvailableDestinations { get; set; } = new List<string>(); // Danh sách các điểm đến
        public bool IsSearching { get; private set; } // Thuộc tính mới để xác định có đang tìm kiếm không

        // Định nghĩa một khoảng thời gian đệm cho việc hủy vé, tương tự như trong trang quản lý
        private readonly TimeSpan _cancellationBuffer = TimeSpan.FromHours(2); // Ví dụ: 2 giờ

        public async Task OnGetAsync()
        {
            // Lấy danh sách các điểm đi và điểm đến duy nhất từ Route
            AvailableDepartures = await _context.Routes.Select(r => r.Departure).Distinct().OrderBy(d => d).ToListAsync();
            AvailableDestinations = await _context.Routes.Select(r => r.Destination).Distinct().OrderBy(d => d).ToListAsync();

            bool isAnyFilterActive = false;
            if (!string.IsNullOrEmpty(Departure)) isAnyFilterActive = true;
            if (!string.IsNullOrEmpty(Destination)) isAnyFilterActive = true;
            if (MinPrice.HasValue) isAnyFilterActive = true;
            if (MaxPrice.HasValue) isAnyFilterActive = true;

            bool departureDateProvided = DepartureDate.HasValue;
            bool returnDateProvided = ReturnDate.HasValue;
            if (departureDateProvided) isAnyFilterActive = true;
            if (returnDateProvided && TripType == "round-trip") isAnyFilterActive = true;


            IQueryable<Trip> baseQuery = _context.Trips
                .Include(t => t.Route)
                .Include(t => t.Bus)
                .Where(t => t.Route != null)
                .Include(t => t.Company)
                .Where(t => t.Status == TripStatus.Scheduled); // Chỉ hiển thị các chuyến đã lên lịch

            if (isAnyFilterActive)
            {
                // Áp dụng các bộ lọc được cung cấp
                if (!string.IsNullOrEmpty(Departure))
                {
                    baseQuery = baseQuery.Where(t => t.Route.Departure.Contains(Departure));
                }
                if (!string.IsNullOrEmpty(Destination))
                {
                    baseQuery = baseQuery.Where(t => t.Route.Destination.Contains(Destination));
                }
            }
            if (TripType == "round-trip" && returnDateProvided)
            {
                if (departureDateProvided)
                {
                    var startOfDay = DepartureDate!.Value.Date;
                    var endOfDay = startOfDay.AddDays(1);
                    IQueryable<Trip> departureQuery = baseQuery.Where(t => t.DepartureTime >= startOfDay && t.DepartureTime < endOfDay);

                    // Áp dụng sắp xếp và lọc giá cho chuyến đi
                    departureQuery = ApplySortingAndPriceFilter(departureQuery);

                    var departureTrips = await departureQuery.AsNoTracking().ToListAsync();
                    AvailableTrips = departureTrips
                        .Select(t => new TripViewModel(t, _cancellationBuffer))
                        .Where(tv => tv.AvailableSeats >= Quantity)
                        .ToList();
                }
                var returnStartOfDay = ReturnDate.Value.Date;
                var returnEndOfDay = returnStartOfDay.AddDays(1);
                IQueryable<Trip> returnQuery = baseQuery.Where(t => t.DepartureTime >= returnStartOfDay && t.DepartureTime < returnEndOfDay);
                if (!string.IsNullOrEmpty(Departure) && !string.IsNullOrEmpty(Destination))
                {
                    returnQuery = returnQuery.Where(t => t.Route.Departure.Contains(Destination) && t.Route.Destination.Contains(Departure));
                }
                returnQuery = ApplySortingAndPriceFilter(returnQuery);
                var returnTrips = await returnQuery.AsNoTracking().ToListAsync();
                AvailableReturnTrips = returnTrips.Select(t => new TripViewModel(t, _cancellationBuffer)).Where(tv => tv.AvailableSeats >= Quantity).ToList();
            }
            else
            {
                IQueryable<Trip> query = baseQuery;
                if (departureDateProvided)
                {
                    var startOfDay = DepartureDate!.Value.Date;
                    var endOfDay = startOfDay.AddDays(1);
                    query = query.Where(t => t.DepartureTime >= startOfDay && t.DepartureTime < endOfDay);
                }
                else if (isAnyFilterActive)
                {
                    query = query.Where(t => t.DepartureTime >= DateTime.Today);
                }
                else
                {
                    query = query.Where(t => t.DepartureTime >= DateTime.Now);
                }
                query = ApplySortingAndPriceFilter(query);

                var trips = await query.AsNoTracking().ToListAsync();
                AvailableTrips = trips
                .Select(t => new TripViewModel(t, _cancellationBuffer))
                .Where(tv => tv.AvailableSeats >= Quantity) // Chỉ hiển thị các chuyến có đủ ghế
                .ToList();
            }
        }
        private IQueryable<Trip> ApplySortingAndPriceFilter(IQueryable<Trip> query)
        {
            if (MinPrice.HasValue)
            {
                query = query.Where(t => t.Price >= MinPrice.Value);
            }
            if (MaxPrice.HasValue)
            {
                query = query.Where(t => t.Price <= MaxPrice.Value);
            }
            switch (SortOrder?.ToLower())
            {
                case "price_asc":
                    query = query.OrderBy(t => t.Price);
                    break;
                case "price_desc":
                    query = query.OrderByDescending(t => t.Price);
                    break;
                case "departure_asc":
                    query = query.OrderBy(t => t.DepartureTime);
                    break;
                case "departure_desc":
                    query = query.OrderByDescending(t => t.DepartureTime);
                    break;
                default:
                    query = query.OrderBy(t => t.DepartureTime);
                    break;
            }
            return query;
        }
        // Trình xử lý để lấy dữ liệu ghế cho một chuyến đi cụ thể
        public async Task<JsonResult> OnGetSeatDataAsync(int tripId)
        {
            var trip = await _context.Trips
                .Include(t => t.Bus)
                .Include(t => t.Tickets.Where(ti => ti.Status == TicketStatus.Booked || ti.Status == TicketStatus.Used)) // Chỉ lấy vé đã đặt/đã sử dụng
                .FirstOrDefaultAsync(t => t.TripId == tripId);

            if (trip == null || trip.Bus == null)
            {
                return new JsonResult(NotFound("Không tìm thấy chuyến đi hoặc xe."));
            }

            var bookedSeats = trip.Tickets.Select(t => t.SeatNumber).ToList(); // Danh sách ghế đã đặt

            return new JsonResult(new { capacity = trip.Bus.Capacity, bookedSeats = bookedSeats }); // Trả về sức chứa và ghế đã đặt
        }

        // Trình xử lý OnPost cho việc đặt vé (sẽ được gọi bằng AJAX từ modal)
        // Đây là một ví dụ đơn giản hóa. Một triển khai thực tế cần xác thực mạnh mẽ,
        // xử lý giao dịch và kiểm tra điều kiện tranh chấp (race condition).
        public async Task<IActionResult> OnPostBookAsync([FromBody] BookingRequest bookingRequest)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return new JsonResult(new { success = false, message = "Dữ liệu không hợp lệ.", errors = errors }) { StatusCode = 400 };
            }

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    var trip = await _context.Trips
                        .Include(t => t.Bus)
                        // .Include(t => t.Tickets.Where(ti => ti.Status == TicketStatus.Booked || ti.Status == TicketStatus.Used)) // Sẽ query lại sau để đảm bảo tính thời sự
                        .FirstOrDefaultAsync(t => t.TripId == bookingRequest.TripId);

                    if (trip == null || trip.Bus == null)
                    {
                        return new JsonResult(new { success = false, message = "Không tìm thấy chuyến đi." }) { StatusCode = 404 };
                    }

                    // Kiểm tra số lượng ghế yêu cầu
                    if (bookingRequest.SelectedSeats.Count != bookingRequest.Quantity)
                    {
                        return new JsonResult(new { success = false, message = "Số lượng ghế đã chọn không khớp với số lượng yêu cầu." }) { StatusCode = 400 };
                    }

                    // Kiểm tra xem các ghế cụ thể đã chọn có còn trống không (kiểm tra race condition quan trọng)
                    var alreadyBookedSeatNumbers = await _context.Tickets
                        .Where(t => t.TripId == bookingRequest.TripId &&
                                    (t.Status == TicketStatus.Booked || t.Status == TicketStatus.Used) &&
                                    bookingRequest.SelectedSeats.Contains(t.SeatNumber))
                        .Select(t => t.SeatNumber)
                        .ToListAsync();
                    if (alreadyBookedSeatNumbers.Any())
                    {
                        return new JsonResult(new { success = false, message = $"Các ghế sau đã được đặt trong lúc bạn thao tác: {string.Join(", ", alreadyBookedSeatNumbers)}. Vui lòng chọn lại." }) { StatusCode = 400 };
                    }
                    // Kiểm tra tổng số ghế còn trống
                    int totalBookedSeatsForThisTrip = await _context.Tickets
                        .CountAsync(t => t.TripId == trip.TripId && (t.Status == TicketStatus.Booked || t.Status == TicketStatus.Used));
                    int actualAvailableSeats = (trip.Bus.Capacity ?? 0) - totalBookedSeatsForThisTrip;

                    if (bookingRequest.SelectedSeats.Count > actualAvailableSeats)
                    {
                        return new JsonResult(new { success = false, message = $"Không đủ ghế trống. Hiện còn {actualAvailableSeats} ghế, bạn yêu cầu {bookingRequest.SelectedSeats.Count} ghế." }) { StatusCode = 400 };
                    }
                    // Tạo Order
                    var order = new Order
                    {
                        UserId = null, // Hoặc lấy từ người dùng đã đăng nhập
                        GuestPhone = bookingRequest.GuestPhone,
                        TotalAmount = bookingRequest.SelectedSeats.Count * trip.Price,
                        Status = OrderStatus.Pending,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        OrderTickets = new List<OrderTicket>() // Khởi tạo
                    };
                    _context.Orders.Add(order);
                    await _context.SaveChangesAsync(); // Lưu Order để lấy OrderId

                    // Tạo Tickets và OrderTickets
                    var ticketsToCreate = new List<Ticket>();
                    var orderTicketsToCreate = new List<OrderTicket>();

                    foreach (var seatNumber in bookingRequest.SelectedSeats)
                    {
                        var ticket = new Ticket
                        {
                            TripId = trip.TripId,
                            OrderId = order.OrderId, // Gán OrderId trực tiếp
                            UserId = order.UserId,
                            SeatNumber = seatNumber,
                            Price = trip.Price,
                            Status = TicketStatus.Booked,
                            BookedAt = DateTime.UtcNow
                        };
                        ticketsToCreate.Add(ticket);
                    }
                    _context.Tickets.AddRange(ticketsToCreate);
                    await _context.SaveChangesAsync();
                    foreach (var createdTicket in ticketsToCreate)
                    {
                        orderTicketsToCreate.Add(new OrderTicket
                        {
                            OrderId = order.OrderId,
                            TicketId = createdTicket.TicketId
                        });
                    }
                    _context.OrderTickets.AddRange(orderTicketsToCreate);

                    // Cập nhật AvailableSeats trên Trip (dựa trên tính toán mới nhất)
                    trip.AvailableSeats = (trip.Bus.Capacity ?? 0) - (totalBookedSeatsForThisTrip + bookingRequest.SelectedSeats.Count);
                    trip.UpdatedAt = DateTime.UtcNow;
                    _context.Trips.Update(trip);

                    await _context.SaveChangesAsync(); // Lưu OrderTickets và cập nhật Trip

                    await transaction.CommitAsync();

                    return new JsonResult(new { success = true, orderId = order.OrderId });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    // Ghi log lỗi ở đây (ví dụ: _logger.LogError(ex, "Lỗi khi đặt vé");)
                    System.Diagnostics.Debug.WriteLine($"Lỗi khi đặt vé: {ex.ToString()}"); // Ghi log ra Output window khi debug
                    return new JsonResult(new { success = false, message = "Đã xảy ra lỗi máy chủ trong quá trình đặt vé. Vui lòng thử lại." }) { StatusCode = 500 };
                }
            }
        }
    }

    // ViewModel cho yêu cầu đặt vé từ frontend
    public class BookingRequest
    {
        [Required]
        public int TripId { get; set; }
        [Required]
        [MinLength(1, ErrorMessage = "Phải chọn ít nhất một ghế.")]
        public List<string> SelectedSeats { get; set; } = new List<string>(); // Danh sách ghế đã chọn
        [Required]
        [Range(1, 10, ErrorMessage = "Số lượng vé không hợp lệ.")]
        public int Quantity { get; set; } // Số lượng vé (nên khớp với selectedSeats.Count)

        // Thông tin hành khách (đơn giản hóa)
        // Email không còn bắt buộc
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
        public string? GuestPhone { get; set; }
        // Thêm các trường thông tin hành khách khác nếu cần
    }
}