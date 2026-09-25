using IdentityService.Data;
using IdentityService.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using DotNetEnv;

var builder = WebApplication.CreateBuilder(args);

Env.TraversePath().Load();

var dbUser = Environment.GetEnvironmentVariable("POSTGRES_USER");
var dbPass = Environment.GetEnvironmentVariable("POSTGRES_PASSWORD");

var connectionString = $"Host=localhost;Database=identitydb;Username={dbUser};Password={dbPass}";

// Configurar PostgreSQL
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// Configurar Identity
builder.Services.AddIdentity<AppUser, IdentityRole<Guid>>(options => 
{
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

builder.Services.AddControllers();

var app = builder.Build();

// Aplicar migrações e seed automaticamente
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<AppDbContext>();
    context.Database.Migrate(); // Corre as migrações no arranque
    await DbSeeder.SeedAsync(services); // Injeta os dados
}