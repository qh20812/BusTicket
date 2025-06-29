using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;

namespace BusTicketSystem.Models.ViewModels
{
    public class TripSearchViewModel
    {
        [Required]
        public string Departure { get; set; } = null!;
        [Required]
        public string Destination { get; set; } = null!;
        [Required]
        public DateTime Date { get; set; }
    }

    public class TripViewModel
    {
        public int TripId { get; set; }
        public int? AvailableSeats { get; set; }
        public int? Capacity { get; set; }

        public string RouteName { get; set; } = "N/A";
        public string BusInfo { get; set; } = "N/A";
        public string DriverName { get; set; } = "N/A";
        public string? Departure { get; set; } = "N/A";
        public string? Destination { get; set; } = "N/A";
        public string BusType { get; set; } = string.Empty;
        public string StatusDisplayName => Status.GetType().GetField(Status.ToString())?.GetCustomAttribute<DisplayAttribute>()?.GetName() ?? Status.ToString();
        public string? CompanyName { get; set; } = string.Empty;
        public string? CancellationReason { get; set; }
        public string? BusLicensePlate { get; set; }
        public decimal Price { get; set; }
        public decimal? Distance { get; set; } // Changed to nullable to match Route.Distance
        public decimal? OldPrice { get; set; }
        public decimal? DiscountPercentage { get; set; }

        public TripStatus Status { get; set; }

        public DateTime DepartureTime { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime EstimatedArrivalTime { get; set; }

        public bool CanCancel { get; private set; }
        public bool CanCompleteManually { get; private set; }
        public bool CanDelete { get; private set; }

        public TimeSpan EstimatedDuration { get; set; }

        public TripViewModel(Trip t, TimeSpan cancellationBuffer)
        {
            TripId = t.TripId;
            if (t.Route != null) {
                RouteName = $"{t.Route.Departure} \u2192 {t.Route.Destination}";
                Departure = t.Route.Departure;
                Destination = t.Route.Destination;
                Distance = t.Route.Distance; // No need for ?? 0 since Distance is now nullable
                EstimatedDuration = t.Route.EstimatedDuration ?? TimeSpan.Zero;
            }
            if (t.Bus != null)
            {
                BusInfo = $"{t.Bus.LicensePlate} ({t.Bus.BusType})";
                BusType = t.Bus.BusType;
                BusLicensePlate = t.Bus.LicensePlate;
                Capacity = t.Bus.Capacity ?? 0;
            }
            if (t.Driver != null) DriverName = t.Driver.Fullname ?? "N/A";
            CompanyName = t.Company?.CompanyName;
            DepartureTime = t.DepartureTime;
            Price = t.Price;
            AvailableSeats = t.AvailableSeats;
            Status = t.Status;
            
            CreatedAt = t.CreatedAt;
            UpdatedAt = t.UpdatedAt;
            CancellationReason = t.CancellationReason;
            CompletedAt = t.CompletedAt;
            EstimatedArrivalTime = t.DepartureTime.Add(EstimatedDuration);

            CanCancel = t.Status == TripStatus.Scheduled && t.DepartureTime > DateTime.UtcNow.Add(cancellationBuffer);
            CanCompleteManually = t.Status == TripStatus.Scheduled && t.DepartureTime < DateTime.UtcNow;
            CanDelete = !t.Tickets.Any(ti => ti.Status == TicketStatus.Booked || ti.Status == TicketStatus.Used) && (t.Status == TripStatus.Scheduled || t.Status == TripStatus.Cancelled);
        }
    }

    public class PromotionViewModel
    {
        public int PromotionId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DiscountType DiscountType { get; set; }
        public decimal DiscountValue { get; set; }
        public decimal MinOrderAmount { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsFirstOrder { get; set; }
        public int? RouteId { get; set; }
    }
}