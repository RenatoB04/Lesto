using DotNetEnv;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Carregar o ficheiro .env da raiz do projeto
Env.TraversePath().Load();

// Configurar Autenticação JWT (Igual ao Identity)
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
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
    };
});

builder.Services.AddAuthorization(options =>
{
    // Política por omissão exige que o utilizador esteja autenticado
    options.AddPolicy("RequireAuth", policy => policy.RequireAuthenticatedUser());
});

// Adicionar o YARP lendo do appsettings.json
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

// Mapear o Gateway
app.MapReverseProxy();

app.Run();