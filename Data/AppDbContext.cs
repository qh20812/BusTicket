using BusTicketSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace BusTicketSystem.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> option) : base(option) { }
        public DbSet<User> Users { get; set; } // Đổi tên DbSet
        public DbSet<Trip> Trips { get; set; }
        public DbSet<Ticket> Tickets { get; set; }
        public DbSet<BusTicketSystem.Models.Route> Routes { get; set; }
        public DbSet<Promotion> Promotions { get; set; }
        public DbSet<Post> Posts { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<OrderTicket> OrderTickets { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<LoginHistory> LoginHistory { get; set; }
        public DbSet<Driver> Drivers { get; set; }
        public DbSet<Cancellation> Cancellations { get; set; }
        public DbSet<BusCompany> BusCompanies { get; set; }
        public DbSet<Bus> Buses { get; set; }
        // public DbSet<RoutePoint> RoutePoints { get; set; }
        public DbSet<TripStop> TripStops { get; set; }
        public DbSet<Stop> Stops{ get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<Menu> Menus { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Stop>()
                .Property(s => s.Status)
                .HasConversion<string>()
                .HasMaxLength(50);

            modelBuilder.Entity<Stop>()
                .HasOne(s => s.Company)
                .WithMany(c => c.Stops)
                .HasForeignKey(s => s.CompanyId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Stop>()
                .HasIndex(s => new { s.StopName, s.Latitude, s.Longitude })
                .IsUnique();
            modelBuilder.Entity<User>().ToTable("User"); 
            modelBuilder.Entity<User>()
                .Property(u => u.Role)
                .HasMaxLength(50);

            modelBuilder.Entity<User>()
                .HasOne(u => u.Company)
                .WithMany()
                .HasForeignKey(u => u.CompanyId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull); 
            // User.Role is string, HasMaxLength(50) is appropriate.

            modelBuilder.Entity<Notification>().ToTable("Notifications"); 
            modelBuilder.Entity<Notification>()
                .Property(n => n.Category)
                .HasConversion<string>()
                .HasMaxLength(50);

            modelBuilder.Entity<Menu>().ToTable("Menus"); 

            modelBuilder.Entity<Post>().ToTable("Posts");
            modelBuilder.Entity<Post>(entity =>
            {
                entity.Property(e => e.Category)
                      .HasConversion<string>()
                      .HasMaxLength(50);

                entity.Property(e => e.Status)
                      .HasConversion<string>()
                      .HasMaxLength(50);
            });

            modelBuilder.Entity<Bus>(entity => {
                entity.Property(b => b.Status)
                    .HasConversion<string>()
                    .HasMaxLength(50);
            });

            modelBuilder.Entity<Trip>(entity => {
                entity.Property(t => t.Status)
                    .HasConversion<string>()
                    .HasMaxLength(50);
            });

            modelBuilder.Entity<TripStop>().HasOne(ts => ts.Trip).WithMany(t => t.TripStops).HasForeignKey(ts => ts.TripId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<TripStop>().HasIndex(ts => new { ts.TripId, ts.StopOrder }).IsUnique();
            modelBuilder.Entity<TripStop>().HasIndex(ts => new { ts.TripId, ts.StationName }).IsUnique();

            // Decimal type configurations to match SQL DECIMAL(10,2)
            modelBuilder.Entity<Order>()
                .Property(o => o.TotalAmount)
                .HasColumnType("decimal(10,2)");

            modelBuilder.Entity<Ticket>()
                .Property(t => t.Price)
                .HasColumnType("decimal(10,2)");

            modelBuilder.Entity<Trip>()
                .Property(t => t.Price)
                .HasColumnType("decimal(10,2)");

            modelBuilder.Entity<Promotion>()
                .Property(p => p.DiscountValue)
                .HasColumnType("decimal(10,2)");
            modelBuilder.Entity<Promotion>()
                .Property(p => p.MinOrderAmount)
                .HasColumnType("decimal(10,2)");

            modelBuilder.Entity<Models.Route>() // Fully qualify if there's ambiguity
                .Property(r => r.Distance)
                .HasColumnType("decimal(10,2)");
            
            modelBuilder.Entity<Models.Route>()
                .Property(r => r.Status)
                .HasConversion<string>() // Store enum as string
                .HasMaxLength(50);       // Max length for the string representation - ALREADY CORRECT

            modelBuilder.Entity<Models.Route>()
                .HasOne(r => r.ProposedByCompany)
                .WithMany()
                .HasForeignKey(r => r.ProposedByCompanyId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull); 

            modelBuilder.Entity<Payment>()
                .Property(p => p.Amount)
                .HasColumnType("decimal(10,2)");
            modelBuilder.Entity<Payment>(entity => {
                entity.Property(p => p.PaymentGateway)
                    .HasConversion<string>()
                    .HasMaxLength(50);
                entity.Property(p => p.Status)
                    .HasConversion<string>()
                    .HasMaxLength(50);
            });

            modelBuilder.Entity<Driver>()
                .Property(d => d.Status)
                .HasConversion<string>()
                .HasMaxLength(50);

            modelBuilder.Entity<Cancellation>(entity =>
            {
                entity.ToTable("Cancellations");
                entity.Property(c => c.RefundedAmount).HasColumnType("decimal(10,2)");

                entity.HasOne(c => c.Ticket)
                      .WithMany(t => t.Cancellations)
                      .HasForeignKey(c => c.TicketId)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(c => c.Order)
                      .WithMany(o => o.Cancellations)
                      .HasForeignKey(c => c.OrderId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(c => c.ProcessedByAdmin)
                      .WithMany()
                      .HasForeignKey(c => c.ProcessedByAdminId)
                      .IsRequired(false)
                      .OnDelete(DeleteBehavior.SetNull);
                entity.Property(c => c.Status)
                    .HasConversion<string>()
                    .HasMaxLength(50);
            });

            // OnDelete behaviors based on SQL
            modelBuilder.Entity<Promotion>()
                .HasOne(p => p.Route)
                .WithMany(r => r.Promotions)
                .HasForeignKey(p => p.RouteId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Bus>()
                .HasOne(b => b.Company)
                .WithMany(c => c.Buses)
                .HasForeignKey(b => b.CompanyId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Driver>()
                .HasOne(d => d.Company)
                .WithMany(c => c.Drivers)
                .HasForeignKey(d => d.CompanyId)
                .IsRequired(false) // Ensures the FK column is NOT NULL
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Notification>()
                .HasOne(n => n.RecipientUser)
                .WithMany(u => u.Notifications) // Assumes User has ICollection<Notification> Notifications
                .HasForeignKey(n => n.RecipientUserId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);
            modelBuilder.Entity<Message>()
                .Property(m => m.AdminNotes)
                .HasColumnType("TEXT");

            // Configure Message-User relationship for RepliedByUserId and ClosedByUserId
            modelBuilder.Entity<Message>()
                .HasOne(m => m.RepliedByUser)
                .WithMany() // Assuming User doesn't have a direct collection for replied messages
                .HasForeignKey(m => m.RepliedByUserId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull); // SQL uses SET NULL
                                                   // If RepliedByUserId is not nullable, this should be Restrict or NoAction.
                                                   // Given SQL is SET NULL, this is fine if RepliedByUserId is int?

            modelBuilder.Entity<Message>()
                .HasOne(m => m.ClosedByUser)
                .WithMany() // Assuming User doesn't have a direct collection for closed messages
                .HasForeignKey(m => m.ClosedByUserId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull); // SQL uses SET NULL

            modelBuilder.Entity<Message>()
                .Property(m => m.Status)
                .HasConversion<string>()
                .HasMaxLength(50);

            modelBuilder.Entity<Trip>()
                .HasOne(t => t.Route)
                .WithMany(r => r.Trips)
                .HasForeignKey(t => t.RouteId)
                .OnDelete(DeleteBehavior.Restrict); // Matches ON DELETE RESTRICT

            modelBuilder.Entity<Trip>()
                .HasOne(t => t.Bus)
                .WithMany(b => b.Trips)
                .HasForeignKey(t => t.BusId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Trip>()
                .HasOne(t => t.Driver)
                .WithMany(d => d.Trips)
                .HasForeignKey(t => t.DriverId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Trip>()
                .HasOne(t => t.Company)
                .WithMany(c => c.Trips)
                .HasForeignKey(t => t.CompanyId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Ticket>()
                .HasOne(t => t.Trip)
                .WithMany(tr => tr.Tickets)
                .HasForeignKey(t => t.TripId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Ticket>()
                .HasOne(t => t.Order)
                .WithMany(o => o.Tickets) // Assuming Order has ICollection<Ticket> Tickets
                .HasForeignKey(t => t.OrderId)
                .OnDelete(DeleteBehavior.Restrict); // SQL has ON DELETE RESTRICT for Tickets.orderId

            modelBuilder.Entity<Ticket>()
               .HasOne(t => t.User)
               .WithMany(u => u.Tickets)
               .HasForeignKey(t => t.UserId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);
            modelBuilder.Entity<Ticket>()
                .Property(t => t.Status)
                .HasConversion<string>()
                .HasMaxLength(50);

            modelBuilder.Entity<OrderTicket>()
                .HasOne(ot => ot.Order)
                .WithMany(o => o.OrderTickets)
                .HasForeignKey(ot => ot.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<OrderTicket>()
                .HasOne(ot => ot.Ticket)
                .WithMany(t => t.OrderTickets)
                .HasForeignKey(ot => ot.TicketId)
                .OnDelete(DeleteBehavior.Restrict); // SQL has ON DELETE RESTRICT for OrderTickets.ticketId

            modelBuilder.Entity<Post>()
                .HasOne(p => p.User)
                .WithMany(u => u.Posts)
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Restrict); // Matches ON DELETE RESTRICT

            modelBuilder.Entity<Menu>()
                .HasOne(m => m.ParentMenu)
                .WithMany(m => m.ChildMenus)
                .HasForeignKey(m => m.ParentId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull); // Matches ON DELETE SET NULL

            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Order)
                .WithMany(o => o.Payments)
                .HasForeignKey(p => p.OrderId)
                .OnDelete(DeleteBehavior.Cascade); // Matches ON DELETE CASCADE

            modelBuilder.Entity<LoginHistory>()
                .HasOne(lh => lh.User)
                .WithMany(u => u.LoginHistory)
                .HasForeignKey(lh => lh.UserId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull); // Matches ON DELETE SET NULL

            modelBuilder.Entity<Order>()
                .HasOne(o => o.Promotion)
                .WithMany(p => p.Orders)
                .HasForeignKey(o => o.PromotionId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);
            modelBuilder.Entity<Order>()
                .HasOne(o => o.User)
                .WithMany(u => u.Orders)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);
            modelBuilder.Entity<Order>()
                .Property(o => o.Status)
                .HasConversion<string>()
                .HasMaxLength(50);

            modelBuilder.Entity<Promotion>(entity => {
                entity.Property(p => p.DiscountType)
                    .HasConversion<string>()
                    .HasMaxLength(50);
                entity.Property(p => p.Status)
                    .HasConversion<string>()
                    .HasMaxLength(50);
            });
            

            modelBuilder.Entity<BusCompany>()
                .Property(bc => bc.Status)
                .HasConversion<string>()
                .HasMaxLength(50); // Added HasMaxLength for consistency
        }
    }
}