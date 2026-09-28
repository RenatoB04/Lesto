using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using OrderService.Data;

var builder = WebApplication.CreateBuilder(args);

// 1. Carregar variáveis do ficheiro .env na raiz do projeto
Env.TraversePath().Load();
var dbUser = Environment.GetEnvironmentVariable("POSTGRES_USER");
var dbPass = Environment.GetEnvironmentVariable("POSTGRES_PASSWORD");
// Usa a variável DB_HOST se existir (no Docker), caso contrário usa localhost
var dbHost = Environment.GetEnvironmentVariable("DB_HOST") ?? "localhost"; 

// 2. Construir a Connection String dinamicamente (Garantir que a BD se chama orderdb)
var connectionString = $"Host={dbHost};Database=orderdb;Username={dbUser};Password={dbPass}";

builder.Services.AddDbContext<OrderDbContext>(options =>
    options.UseNpgsql(connectionString));

// 3. Adicionar suporte para Controladores
builder.Services.AddControllers();

var app = builder.Build();

// 4. Aplicar migrações automaticamente no arranque
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
    context.Database.Migrate();
}

app.UseAuthorization();
app.MapControllers(); // Ativar as rotas dos controladores

app.Run();