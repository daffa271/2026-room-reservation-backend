using Microsoft.EntityFrameworkCore;
using RoomReservation.Api.Entities;

namespace RoomReservation.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<Booking> Bookings => Set<Booking>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // --- Data Seeding (data awal) ---
        modelBuilder.Entity<Room>().HasData(
            new Room { Id = 1, Name = "Ruang 301", Location = "Gedung A Lt 3", Capacity = 30, IsActive = true },
            new Room { Id = 2, Name = "Lab Jaringan", Location = "Gedung B Lt 2", Capacity = 25, IsActive = true },
            new Room { Id = 3, Name = "Aula Kampus", Location = "Gedung Utama", Capacity = 200, IsActive = true }
        );
    }
}
