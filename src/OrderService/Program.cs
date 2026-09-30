using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using OrderService.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// 1. Carregar variáveis do ficheiro .env na raiz do projeto
Env.TraversePath().Load();
var dbUser = Environment.GetEnvironmentVariable("POSTGRES_USER");
var dbPass = Environment.GetEnvironmentVariable("POSTGRES_PASSWORD");
// Usa a variável DB_HOST se existir (no Docker), caso contrário usa localhost
var dbHost = Environment.GetEnvironmentVariable("DB_HOST") ?? "localhost"; 

// 2. Construir a Connection String dinamicamente
var connectionString = $"Host={dbHost};Database=orderdb;Username={dbUser};Password={dbPass}";

builder.Services.AddDbContext<OrderDbContext>(options =>
    options.UseNpgsql(connectionString));

// 3. Configurar os Clientes HTTP (OSRM e Identity Service)

// Cliente para o OSRM (Com o User-Agent atualizado para a UPCA)
builder.Services.AddHttpClient<IOsrmService, OsrmService>(client =>
{
    // O servidor partilhado do OSRM bane pedidos sem User-Agent válido
    client.DefaultRequestHeaders.Add("User-Agent", "LestoApp_UPCA/1.0");
});

// Cliente para o Identity Service (Necessário para a validação do Estafeta)
var identityUrl = Environment.GetEnvironmentVariable("IDENTITY_API_URL") ?? "http://localhost:5001";
builder.Services.AddHttpClient<IIdentityServiceClient, IdentityServiceClient>(client =>
{
    client.BaseAddress = new Uri(identityUrl);
});

// 4. Adicionar suporte para Controladores
builder.Services.AddControllers();

// 5. Configurar Autenticação JWT
// Adicionado um valor de recurso (fallback) para evitar que a app quebre se o .env falhar a carregar
var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET") ?? "LestoSuperSecretaChaveDeAutenticacao2026!";

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

// Configurar Swagger com suporte para JWT
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.OpenApiInfo 
    { 
        Title = "Lesto Order API", 
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
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Lesto Order API v1"));

// 6. Aplicar migrações automaticamente no arranque
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
    context.Database.Migrate();
}

// 7. Configurar o Pipeline HTTP
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();