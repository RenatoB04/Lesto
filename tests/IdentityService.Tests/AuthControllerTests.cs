using IdentityService.Controllers;
using IdentityService.DTOs;
using IdentityService.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace IdentityService.Tests;

public class AuthControllerTests
{
    [Fact]
    public async Task Register_NovoRegisto_GaranteRoleCliente()
    {
        // 1. Preparação
        var mockUserManager = MockHelper.MockUserManager();
        var mockConfig = new Mock<IConfiguration>();
        
        mockUserManager.Setup(x => x.CreateAsync(It.IsAny<AppUser>(), It.IsAny<string>()))
                       .ReturnsAsync(Microsoft.AspNetCore.Identity.IdentityResult.Success);

        var controller = new AuthController(mockUserManager.Object, mockConfig.Object);
        var dto = new RegisterDto { Email = "teste@lesto.pt", Password = "123", DisplayName = "Teste" };

        // 2. Ação
        var result = await controller.Register(dto);

        // 3. Verificação
        Assert.IsType<OkObjectResult>(result);
        
        // Verifica se o método AddToRoleAsync foi chamado com a role "Cliente"
        mockUserManager.Verify(x => x.AddToRoleAsync(It.IsAny<AppUser>(), "Cliente"), Times.Once);
    }

    [Fact]
    public async Task Login_CredenciaisInvalidas_DevolveUnauthorized()
    {
        // 1. Preparação
        var mockUserManager = MockHelper.MockUserManager();
        var mockConfig = new Mock<IConfiguration>();

        // Simular que o utilizador existe, mas a password está errada
        var fakeUser = new AppUser { Email = "teste@lesto.pt" };
        mockUserManager.Setup(x => x.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync(fakeUser);
        mockUserManager.Setup(x => x.CheckPasswordAsync(fakeUser, It.IsAny<string>())).ReturnsAsync(false);

        var controller = new AuthController(mockUserManager.Object, mockConfig.Object);
        var dto = new LoginDto { Email = "teste@lesto.pt", Password = "PasswordErrada" };

        // 2. Ação
        var result = await controller.Login(dto);

        // 3. Verificação
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.NotNull(unauthorizedResult.Value);
    }
}