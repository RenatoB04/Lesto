using System.Text;
using DotNetEnv;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PaymentService.Data;
using PaymentService.Services;
using Stripe;

var builder = WebApplication.CreateBuilder(args);

// 1. Carregar variáveis do ficheiro .env na raiz do projeto
Env.TraversePath().Load();

static string RequireEnv(string name) =>
    Environment.GetEnvironmentVariable(name) is { Length: > 0 } value
        ? value
        : throw new InvalidOperationException($"{name} não está definida.");

var dbUser = Environment.GetEnvironmentVariable("POSTGRES_USER");
var dbPass = Environment.GetEnvironmentVariable("POSTGRES_PASSWORD");
var dbHost = Environment.GetEnvironmentVariable("DB_HOST") ?? "localhost";
var connectionString = $"Host={dbHost};Database=paymentdb;Username={dbUser};Password={dbPass}";

builder.Services.AddDbContext<PaymentDbContext>(options => options.UseNpgsql(connectionString));

// 2. Configurar o Stripe (chave de teste lida do .env)
var stripeKey = RequireEnv("STRIPE_SECRET_KEY");
if (!stripeKey.StartsWith("sk_test_"))
{
    // Garantir que nunca se usa uma chave real
    throw new InvalidOperationException("STRIPE_SECRET_KEY tem de ser uma chave de teste (sk_test_...).");
}
builder.Services.AddSingleton<IStripeClient>(new StripeClient(stripeKey));
builder.Services.AddScoped<IStripeGateway, StripeGateway>();

// 3. Cliente para o Order Service (endpoints internos)
RequireEnv("INTERNAL_API_KEY");
var orderInternalUrl = Environment.GetEnvironmentVariable("ORDER_INTERNAL_URL") ?? "http://localhost:5002";
builder.Services.AddHttpClient<IOrderServiceClient, OrderServiceClient>(client =>
{
    client.BaseAddress = new Uri(orderInternalUrl);
    client.Timeout = TimeSpan.FromSeconds(10);
});

builder.Services.AddScoped<PaymentConfirmationService>();
builder.Services.AddControllers();

// 4. Configurar Autenticação JWT (igual aos outros serviços)
var jwtSecret = RequireEnv("JWT_SECRET");
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = "LestoIdentityService",
        ValidAudience = "LestoApiGateway",
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        RoleClaimType = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"
    };
});

builder.Services.AddAuthorization();
builder.Services.AddHealthChecks();

// 5. Configurar Swagger com suporte para JWT
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.OpenApiInfo
    {
        Title = "Lesto Payment API",
        Version = "v1"
    });

    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.OpenApiSecurityScheme
    {
        Description = "Insira o token JWT desta forma: Bearer {token}",
        Name = "Authorization",
        In = Microsoft.OpenApi.ParameterLocation.Header,
        Type = Microsoft.OpenApi.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    c.AddSecurityRequirement(document => new Microsoft.OpenApi.OpenApiSecurityRequirement
    {
        [new Microsoft.OpenApi.OpenApiSecuritySchemeReference("Bearer", document)] = new List<string>()
    });
});

var app = builder.Build();

// Ativar Swagger
app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Lesto Payment API v1"));

// 6. Aplicar migrações automaticamente no arranque
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
    context.Database.Migrate();
}

// 7. Configurar o Pipeline HTTP
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health/live");

app.Run();
