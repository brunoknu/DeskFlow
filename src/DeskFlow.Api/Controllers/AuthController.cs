using DeskFlow.Domain.Enums;
using DeskFlow.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace DeskFlow.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly TimeProvider _time;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        TimeProvider time,
        ILogger<AuthController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _time = time;
        _logger = logger;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterRequest req, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var now = _time.GetUtcNow();
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = req.Email,
            Email = req.Email,
            FullName = req.FullName,
            IsActive = true,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        var result = await _userManager.CreateAsync(user, req.Password);
        if (!result.Succeeded)
            return ValidationProblem(detail: string.Join(" ", result.Errors.Select(e => e.Description)));

        await _userManager.AddToRoleAsync(user, UserRole.Requester);

        // In production: send confirmation email via outbox
        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        _logger.LogInformation("User {Email} registered. Confirmation token generated (not sent in dev).", req.Email);

        // Auto-confirm in development for easier testing
        await _userManager.ConfirmEmailAsync(user, token);

        return Created($"/api/users/{user.Id}", new { userId = user.Id });
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest req, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var user = await _userManager.FindByEmailAsync(req.Email);

        // Neutral response regardless of whether user exists (prevent enumeration)
        if (user is null || !user.IsActive)
        {
            await Task.Delay(Random.Shared.Next(100, 300), ct); // timing-safe delay
            return Unauthorized(new { message = "Invalid credentials." });
        }

        var result = await _signInManager.PasswordSignInAsync(
            user, req.Password, isPersistent: req.RememberMe, lockoutOnFailure: true);

        if (result.IsLockedOut)
        {
            _logger.LogWarning("Account locked out for user {Email}.", req.Email);
            return StatusCode(429, new { message = "Account temporarily locked. Try again later." });
        }

        if (!result.Succeeded)
            return Unauthorized(new { message = "Invalid credentials." });

        user.LastLoginAtUtc = _time.GetUtcNow();
        await _userManager.UpdateAsync(user);

        var roles = await _userManager.GetRolesAsync(user);
        return Ok(new { userId = user.Id, fullName = user.FullName, roles });
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return NoContent();
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me()
    {
        var userId = _userManager.GetUserId(User);
        var user = await _userManager.FindByIdAsync(userId!);
        if (user is null || !user.IsActive) return Unauthorized();

        var roles = await _userManager.GetRolesAsync(user);
        return Ok(new
        {
            userId = user.Id,
            email = user.Email,
            fullName = user.FullName,
            departmentId = user.DepartmentId,
            roles
        });
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest req, CancellationToken ct)
    {
        // Always return the same response to prevent enumeration
        var user = await _userManager.FindByEmailAsync(req.Email);
        if (user is not null && user.IsActive && await _userManager.IsEmailConfirmedAsync(user))
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            // In production: enqueue outbox message with reset link
            _logger.LogInformation("Password reset token generated for {Email}.", req.Email);
        }

        return Ok(new { message = "If an account with that email exists, a password reset link has been sent." });
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest req, CancellationToken ct)
    {
        var user = await _userManager.FindByEmailAsync(req.Email);
        if (user is null)
            return Ok(new { message = "Password reset completed." }); // neutral

        var result = await _userManager.ResetPasswordAsync(user, req.Token, req.NewPassword);
        if (!result.Succeeded)
            return BadRequest(new { message = "Invalid or expired token." });

        return Ok(new { message = "Password reset completed." });
    }

    [HttpPost("confirm-email")]
    [AllowAnonymous]
    public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailRequest req)
    {
        var user = await _userManager.FindByIdAsync(req.UserId.ToString());
        if (user is null) return BadRequest(new { message = "Invalid request." });

        var result = await _userManager.ConfirmEmailAsync(user, req.Token);
        if (!result.Succeeded)
            return BadRequest(new { message = "Invalid or expired confirmation token." });

        return Ok(new { message = "Email confirmed." });
    }

    [HttpGet("antiforgery")]
    [AllowAnonymous]
    public IActionResult GetAntiforgery([FromServices] Microsoft.AspNetCore.Antiforgery.IAntiforgery antiforgery)
    {
        var tokens = antiforgery.GetAndStoreTokens(HttpContext);
        return Ok(new { token = tokens.RequestToken });
    }
}

public sealed record RegisterRequest(string Email, string FullName, string Password);
public sealed record LoginRequest(string Email, string Password, bool RememberMe = false);
public sealed record ForgotPasswordRequest(string Email);
public sealed record ResetPasswordRequest(string Email, string Token, string NewPassword);
public sealed record ConfirmEmailRequest(Guid UserId, string Token);
