using IdentityService.Models;
using Microsoft.AspNetCore.Identity;

namespace IdentityService.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var userManager = serviceProvider.GetRequiredService<UserManager<AppUser>>();

        string[] roles = { "Administrador", "Gestor", "Estafeta", "Cliente" };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                var result = await roleManager.CreateAsync(new IdentityRole<Guid>(role));
                if (!result.Succeeded)
                {
                    throw new InvalidOperationException(
                        $"Não foi possível criar a role {role}: {string.Join(", ", result.Errors.Select(error => error.Description))}");
                }
            }
        }

        var seedUsers = new[]
        {
            new SeedUser("Administrador", "ADMIN", "Administrador Lesto"),
            new SeedUser("Gestor", "GESTOR", "Gestor Lesto"),
            new SeedUser("Estafeta", "ESTAFETA", "Estafeta Lesto"),
            new SeedUser("Cliente", "CLIENTE", "Cliente Lesto")
        };

        foreach (var seedUser in seedUsers)
        {
            var email = Environment.GetEnvironmentVariable($"{seedUser.EnvironmentPrefix}_EMAIL");
            var password = Environment.GetEnvironmentVariable($"{seedUser.EnvironmentPrefix}_PASSWORD");

            if (string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(password))
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                throw new InvalidOperationException(
                    $"{seedUser.EnvironmentPrefix}_EMAIL e {seedUser.EnvironmentPrefix}_PASSWORD têm de ser configurados em conjunto.");
            }

            await EnsureUserAsync(userManager, seedUser, email, password);
        }
    }

    private static async Task EnsureUserAsync(
        UserManager<AppUser> userManager,
        SeedUser seedUser,
        string email,
        string password)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user != null)
        {
            if (!await userManager.IsInRoleAsync(user, seedUser.Role))
            {
                throw new InvalidOperationException(
                    $"O utilizador configurado em {seedUser.EnvironmentPrefix}_EMAIL já existe, mas não tem a role {seedUser.Role}.");
            }

            return;
        }

        user = new AppUser
        {
            UserName = email,
            Email = email,
            DisplayName = seedUser.DisplayName
        };

        var createResult = await userManager.CreateAsync(user, password);
        if (!createResult.Succeeded)
        {
            throw new InvalidOperationException(
                $"Não foi possível criar o utilizador {seedUser.Role}: {string.Join(", ", createResult.Errors.Select(error => error.Description))}");
        }

        var roleResult = await userManager.AddToRoleAsync(user, seedUser.Role);
        if (!roleResult.Succeeded)
        {
            throw new InvalidOperationException(
                $"Não foi possível atribuir a role {seedUser.Role}: {string.Join(", ", roleResult.Errors.Select(error => error.Description))}");
        }
    }

    private sealed record SeedUser(string Role, string EnvironmentPrefix, string DisplayName);
}