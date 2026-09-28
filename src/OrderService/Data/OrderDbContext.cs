using Microsoft.EntityFrameworkCore;
using OrderService.Models;

namespace OrderService.Data;

public class OrderDbContext : DbContext
{
    public OrderDbContext(DbContextOptions<OrderDbContext> options) : base(options) { }

    public DbSet<Order> Orders => Set<Order>();
    public DbSet<Point> Points => Set<Point>();
    public DbSet<Setting> Settings => Set<Setting>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Setting>().HasKey(s => s.Key);

        modelBuilder.Entity<Order>()
            .HasOne(o => o.Origin)
            .WithMany()
            .HasForeignKey(o => o.OriginPointId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Order>()
            .HasOne(o => o.Destination)
            .WithMany()
            .HasForeignKey(o => o.DestinationPointId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Setting>().HasData(
            new Setting { Key = "PesoMaximoKg", Value = "30" }
        );

        modelBuilder.Entity<Point>().HasData(
            new Point { Id = Guid.Parse("11111111-1111-1111-1111-111111111111"), Name = "Ponto Braga Centro", Latitude = 41.5503, Longitude = -8.4200 },
            new Point { Id = Guid.Parse("22222222-2222-2222-2222-222222222222"), Name = "Ponto Universidade Minho", Latitude = 41.5614, Longitude = -8.3973 },
            new Point { Id = Guid.Parse("33333333-3333-3333-3333-333333333333"), Name = "Ponto Barcelos", Latitude = 41.5360, Longitude = -8.6251 },
            new Point { Id = Guid.Parse("44444444-4444-4444-4444-444444444444"), Name = "Ponto IPCA", Latitude = 41.5367, Longitude = -8.6277 },
            new Point { Id = Guid.Parse("55555555-5555-5555-5555-555555555555"), Name = "Ponto Famalicão", Latitude = 41.4079, Longitude = -8.5193 },
            new Point { Id = Guid.Parse("66666666-6666-6666-6666-666666666666"), Name = "Ponto Guimarães", Latitude = 41.4444, Longitude = -8.2961 }
        );
    }
}