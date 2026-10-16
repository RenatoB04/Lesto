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
                await roleManager.CreateAsync(new IdentityRole<Guid>(role));
            }
        }

        var adminEmail = Environment.GetEnvironmentVariable("ADMIN_EMAIL");
        var adminPassword = Environment.GetEnvironmentVariable("ADMIN_PASSWORD");

        if (string.IsNullOrWhiteSpace(adminEmail) && string.IsNullOrWhiteSpace(adminPassword))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
        {
            throw new InvalidOperationException(
                "ADMIN_EMAIL e ADMIN_PASSWORD têm de ser configurados em conjunto.");
        }

        var existingAdmin = await userManager.FindByEmailAsync(adminEmail);
        if (existingAdmin != null)
        {
            if (!await userManager.IsInRoleAsync(existingAdmin, "Administrador"))
            {
                throw new InvalidOperationException(
                    "O utilizador configurado em ADMIN_EMAIL já existe, mas não tem a role Administrador.");
            }

            return;
        }

        var admin = new AppUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            DisplayName = "Administrador Lesto"
        };
        var createResult = await userManager.CreateAsync(admin, adminPassword);
        if (!createResult.Succeeded)
        {
            throw new InvalidOperationException(
                $"Não foi possível criar o administrador inicial: {string.Join(", ", createResult.Errors.Select(error => error.Description))}");
        }

        var roleResult = await userManager.AddToRoleAsync(admin, "Administrador");
        if (!roleResult.Succeeded)
        {
            throw new InvalidOperationException(
                $"Não foi possível atribuir a role Administrador: {string.Join(", ", roleResult.Errors.Select(error => error.Description))}");
        }
    }
}