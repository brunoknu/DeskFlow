using DeskFlow.Api.Policies;
using DeskFlow.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DeskFlow.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Policy = AuthorizationPolicies.CanManageUsers)]
public class UsersController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;

    public UsersController(UserManager<ApplicationUser> userManager) => _userManager = userManager;

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var users = await _userManager.Users
            .Select(u => new
            {
                u.Id, u.Email, u.FullName, u.IsActive,
                u.DepartmentId, u.LastLoginAtUtc, u.CreatedAtUtc
            })
            .ToListAsync(ct);
        return Ok(users);
    }

    [HttpPost("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null) return NotFound();
        user.IsActive = false;
        user.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await _userManager.UpdateAsync(user);
        await _userManager.UpdateSecurityStampAsync(user); // invalidates existing sessions
        return NoContent();
    }

    [HttpPost("{id:guid}/activate")]
    public async Task<IActionResult> Activate(Guid id, CancellationToken ct)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null) return NotFound();
        user.IsActive = true;
        user.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await _userManager.UpdateAsync(user);
        return NoContent();
    }
}
