using IdentityService.Models;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace IdentityService.Tests;

public static class MockHelper
{
    public static Mock<UserManager<AppUser>> MockUserManager()
    {
        var store = new Mock<IUserStore<AppUser>>();
        return new Mock<UserManager<AppUser>>(store.Object, null, null, null, null, null, null, null, null);
    }

    public static Mock<RoleManager<IdentityRole<Guid>>> MockRoleManager()
    {
        var store = new Mock<IRoleStore<IdentityRole<Guid>>>();
        return new Mock<RoleManager<IdentityRole<Guid>>>(store.Object, null, null, null, null);
    }
}