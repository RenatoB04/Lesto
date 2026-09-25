using Microsoft.AspNetCore.Identity;

namespace IdentityService.Models;

public class AppUser : IdentityUser<Guid>
{
    public string DisplayName { get; set; } = string.Empty;
}