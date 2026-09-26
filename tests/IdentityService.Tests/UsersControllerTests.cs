using System.Security.Claims;
using IdentityService.Controllers;
using IdentityService.DTOs;
using IdentityService.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace IdentityService.Tests;

public class UsersControllerTests
{
    [Fact]
    public async Task CreateUser_GestorTentaCriarGestor_DevolveForbid()
    {
        // 1. Preparação (Arrange)
        var mockUserManager = MockHelper.MockUserManager();
        var mockRoleManager = MockHelper.MockRoleManager();
        var controller = new UsersController(mockUserManager.Object, mockRoleManager.Object);

        // Simular que quem está a fazer o pedido é um "Gestor"
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, "Gestor") }));
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = user } };

        var dto = new CreateUserDto { Email = "novo@lesto.pt", Password = "123", Role = "Gestor" };

        // 2. Ação (Act)
        var result = await controller.CreateUser(dto);

        // 3. Verificação (Assert)
        Assert.IsType<ForbidResult>(result); // Tem de ser bloqueado (HTTP 403)
    }

    [Fact]
    public async Task CreateUser_GestorTentaCriarEstafeta_DevolveCreated()
    {
        // 1. Preparação (Arrange)
        var mockUserManager = MockHelper.MockUserManager();
        var mockRoleManager = MockHelper.MockRoleManager();

        // Simular que a role existe e a criação tem sucesso
        mockRoleManager.Setup(x => x.RoleExistsAsync("Estafeta")).ReturnsAsync(true);
        mockUserManager.Setup(x => x.CreateAsync(It.IsAny<AppUser>(), It.IsAny<string>()))
                       .ReturnsAsync(Microsoft.AspNetCore.Identity.IdentityResult.Success);

        var controller = new UsersController(mockUserManager.Object, mockRoleManager.Object);

        var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, "Gestor") }));
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = user } };

        var dto = new CreateUserDto { Email = "novo@lesto.pt", Password = "123", Role = "Estafeta" };

        // 2. Ação (Act)
        var result = await controller.CreateUser(dto);

        // 3. Verificação (Assert)
        Assert.IsType<CreatedAtActionResult>(result); // Sucesso (HTTP 201)
    }
}