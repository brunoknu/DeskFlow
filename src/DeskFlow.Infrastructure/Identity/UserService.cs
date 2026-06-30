using DeskFlow.Application.Contracts;
using DeskFlow.Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace DeskFlow.Infrastructure.Identity;

public class UserService : IUserService
{
    private readonly UserManager<ApplicationUser> _userManager;

    public UserService(UserManager<ApplicationUser> userManager)
        => _userManager = userManager;

    public async Task<bool> IsActiveAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        return user is not null && user.IsActive;
    }

    public async Task<bool> IsAgentOrManagerAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null || !user.IsActive) return false;

        return await _userManager.IsInRoleAsync(user, UserRole.Agent)
            || await _userManager.IsInRoleAsync(user, UserRole.Manager);
    }
}
