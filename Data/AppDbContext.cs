using Microsoft.EntityFrameworkCore;
using SafeWayAPI.Models;

namespace SafeWayAPI.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }

        public DbSet<Subscription> Subscriptions { get; set; }

        public DbSet<Station> Stations{ get; set; }
        public DbSet<StationChangeRequest> StationChangeRequests { get; set; }
    }
}