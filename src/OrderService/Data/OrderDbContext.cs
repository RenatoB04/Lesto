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
            new Point { Id = Guid.Parse("d8b3c9a2-5f6e-4b1a-8c2d-9e7f3a1b4c6d"), Name = "Ponto Braga Centro", Latitude = 41.5503, Longitude = -8.4200 },
            new Point { Id = Guid.Parse("a1f4e7c2-9b8d-4a3f-6c5e-2b1d9a8c7e3f"), Name = "Ponto Universidade Minho", Latitude = 41.5614, Longitude = -8.3973 },
            new Point { Id = Guid.Parse("c3e2d1f4-8a9b-4c7e-5d6f-3b2a1c9e8d7f"), Name = "Ponto Barcelos", Latitude = 41.5360, Longitude = -8.6251 },
            new Point { Id = Guid.Parse("f9e8d7c6-b5a4-4f3e-2d1c-9b8a7c6e5d4f"), Name = "Ponto IPCA", Latitude = 41.5367, Longitude = -8.6277 },
            new Point { Id = Guid.Parse("e5d4c3b2-a1f9-4e8d-7c6b-5a4f3e2d1c9b"), Name = "Ponto Famalicão", Latitude = 41.4079, Longitude = -8.5193 },
            new Point { Id = Guid.Parse("b2a1c9e8-d7f6-4e5d-8c9b-0a1f2e3d4c5b"), Name = "Ponto Guimarães", Latitude = 41.4444, Longitude = -8.2961 }
        );
    }
}