using IdentityService.DTOs;
using IdentityService.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IdentityService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Obriga a estar autenticado por defeito em todos os métodos
public class UsersController : ControllerBase
{
    private readonly UserManager<AppUser> _userManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;

    public UsersController(UserManager<AppUser> userManager, RoleManager<IdentityRole<Guid>> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    // GET /api/users?role=Estafeta
    [HttpGet]
    [Authorize(Roles = "Administrador, Gestor")]
    public async Task<IActionResult> GetUsers([FromQuery] string? role)
    {
        var users = await _userManager.Users.ToListAsync();
        var userDtos = new List<UserDto>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var userRole = roles.FirstOrDefault() ?? "Cliente";

            // Se o filtro de role foi enviado e não corresponde, ignora este utilizador
            if (!string.IsNullOrEmpty(role) && !userRole.Equals(role, StringComparison.OrdinalIgnoreCase))
                continue;

            userDtos.Add(new UserDto
            {
                Id = user.Id,
                Email = user.Email!,
                DisplayName = user.DisplayName,
                Role = userRole
            });
        }

        return Ok(userDtos);
    }

    // GET /api/users/{id}
    [HttpGet("{id}")]
    [Authorize(Roles = "Administrador, Gestor")]
    public async Task<IActionResult> GetUserById(Guid id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null)
            return NotFound(new { Message = "Utilizador não encontrado." });

        var roles = await _userManager.GetRolesAsync(user);
        
        var userDto = new UserDto
        {
            Id = user.Id,
            Email = user.Email!,
            DisplayName = user.DisplayName,
            Role = roles.FirstOrDefault() ?? "Cliente"
        };

        return Ok(userDto);
    }

    // POST /api/users
    [HttpPost]
    [Authorize(Roles = "Administrador, Gestor")]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserDto dto)
    {
        // Regra de negócio: O Gestor só pode criar Estafetas
        if (User.IsInRole("Gestor") && !dto.Role.Equals("Estafeta", StringComparison.OrdinalIgnoreCase))
        {
            // Forbid devolve o erro HTTP 403 (Proibido)
            return Forbid(); 
        }

        // Verificar se a role pedida existe na base de dados
        if (!await _roleManager.RoleExistsAsync(dto.Role))
        {
            return BadRequest(new { Message = $"O perfil '{dto.Role}' não existe." });
        }

        var user = new AppUser
        {
            UserName = dto.Email,
            Email = dto.Email,
            DisplayName = dto.DisplayName
        };

        var result = await _userManager.CreateAsync(user, dto.Password);

        if (!result.Succeeded)
            return BadRequest(result.Errors);

        await _userManager.AddToRoleAsync(user, dto.Role);

        return CreatedAtAction(nameof(GetUserById), new { id = user.Id }, new { Message = "Utilizador criado com sucesso." });
    }

    // DELETE /api/users/{id}
    [HttpDelete("{id}")]
    [Authorize(Roles = "Administrador")] // Apenas o Administrador pode eliminar
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null)
            return NotFound(new { Message = "Utilizador não encontrado." });

        // Medida de segurança extra: impedir que o administrador se elimine a si próprio
        var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (currentUserId == id.ToString())
            return BadRequest(new { Message = "Não podes eliminar a tua própria conta." });

        var result = await _userManager.DeleteAsync(user);

        if (!result.Succeeded)
            return BadRequest(result.Errors);

        return Ok(new { Message = "Utilizador eliminado com sucesso." });
    }
}