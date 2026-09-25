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

        // Criar um utilizador para cada perfil, caso não existam
        await CreateUserIfNotExists(userManager, "admin@lesto.pt", "Admin123", "Administrador Lesto", "Administrador");
        await CreateUserIfNotExists(userManager, "gestor@lesto.pt", "Gestor123", "Gestor de Frota", "Gestor");
        await CreateUserIfNotExists(userManager, "estafeta@lesto.pt", "Estafeta123", "João Estafeta", "Estafeta");
        await CreateUserIfNotExists(userManager, "cliente@lesto.pt", "Cliente123", "Maria Cliente", "Cliente");
    }

    private static async Task CreateUserIfNotExists(UserManager<AppUser> userManager, string email, string password, string displayName, string role)
    {
        if (await userManager.FindByEmailAsync(email) == null)
        {
            var user = new AppUser { UserName = email, Email = email, DisplayName = displayName };
            var result = await userManager.CreateAsync(user, password);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(user, role);
            }
        }
    }
}