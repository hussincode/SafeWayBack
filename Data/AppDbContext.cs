using Microsoft.EntityFrameworkCore;
using SafeWayAPI.Models;

namespace SafeWayAPI.Data
{
    public class AppDbContext : DbContext
    {

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }
       public DbSet<User>                 Users                 { get; set; }
        public DbSet<Subscription>         Subscriptions         { get; set; }
        public DbSet<Station>              Stations              { get; set; }
        public DbSet<BusRoute>                Routes                { get; set; }
        public DbSet<RouteStation>         RouteStations         { get; set; }
        public DbSet<StationChangeRequest> StationChangeRequests { get; set; }
        public DbSet<StationChangeRequest>   RouteChangeRequests   { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Map entities to lowercase table names to match PostgreSQL convention
            modelBuilder.Entity<User>().ToTable("users");
            modelBuilder.Entity<Subscription>().ToTable("subscriptions");
            modelBuilder.Entity<Station>().ToTable("stations");
            modelBuilder.Entity<BusRoute>().ToTable("routes");
            modelBuilder.Entity<RouteStation>().ToTable("routestations");
            modelBuilder.Entity<StationChangeRequest>().ToTable("stationchangerequests");

            // Map properties to lowercase column names
            modelBuilder.Entity<User>(entity =>
            {
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.UniqueID).HasColumnName("uniqueid");
                entity.Property(e => e.Password).HasColumnName("password");
                entity.Property(e => e.FullName).HasColumnName("fullname");
                entity.Property(e => e.Role).HasColumnName("role");
                entity.Property(e => e.Grade).HasColumnName("grade");
                entity.Property(e => e.ParentId).HasColumnName("parentid");
                entity.Property(e => e.BusNumber).HasColumnName("busnumber");
                entity.Property(e => e.DriverName).HasColumnName("drivername");
                entity.Property(e => e.RouteName).HasColumnName("routename");
                entity.Property(e => e.StopName).HasColumnName("stopname");
                entity.Property(e => e.CreatedAt).HasColumnName("createdat");
            });

            modelBuilder.Entity<Subscription>(entity =>
            {
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.UserId).HasColumnName("userid");
                entity.Property(e => e.Status).HasColumnName("status");
                entity.Property(e => e.StartDate).HasColumnName("startdate");
                entity.Property(e => e.EndDate).HasColumnName("enddate");
                entity.Property(e => e.CreatedAt).HasColumnName("createdat");
            });

            modelBuilder.Entity<Station>(entity =>
            {
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Name).HasColumnName("name");
                entity.Property(e => e.IsActive).HasColumnName("isactive");
            });

            modelBuilder.Entity<BusRoute>(entity =>
            {
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Name).HasColumnName("name");
                entity.Property(e => e.IsActive).HasColumnName("isactive");
            });

            modelBuilder.Entity<RouteStation>(entity =>
            {
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.RouteId).HasColumnName("routeid");
                entity.Property(e => e.StationId).HasColumnName("stationid");
                entity.Property(e => e.StopOrder).HasColumnName("stoporder");
                entity.Property(e => e.PickupTime).HasColumnName("pickuptime");
            });

            modelBuilder.Entity<StationChangeRequest>(entity =>
            {
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.UserId).HasColumnName("userid");
                entity.Property(e => e.NewStationId).HasColumnName("newstationid");
                entity.Property(e => e.EffectiveDate).HasColumnName("effectivedate");
                entity.Property(e => e.Status).HasColumnName("status");
                entity.Property(e => e.AdminNote).HasColumnName("adminnote");
                entity.Property(e => e.CreatedAt).HasColumnName("createdat");
            });
        }
    }
}