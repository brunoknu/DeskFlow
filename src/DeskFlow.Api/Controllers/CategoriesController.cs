using DeskFlow.Api.Policies;
using DeskFlow.Domain.Entities;
using DeskFlow.Domain.Enums;
using DeskFlow.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DeskFlow.Api.Controllers;

[ApiController]
[Route("api/categories")]
[Authorize]
public class CategoriesController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public CategoriesController(ApplicationDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var cats = await _db.TicketCategories
            .Where(c => c.IsActive)
            .Select(c => new { c.Id, c.Name, c.Description, c.DefaultPriority })
            .ToListAsync(ct);
        return Ok(cats);
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.CanManageDepartments)]
    public async Task<IActionResult> Create([FromBody] CategoryRequest req, CancellationToken ct)
    {
        var cat = TicketCategory.Create(req.Name, req.Description, req.DefaultPriority);
        _db.TicketCategories.Add(cat);
        await _db.SaveChangesAsync(ct);
        return Created($"/api/categories/{cat.Id}", new { id = cat.Id });
    }
}

public sealed record CategoryRequest(string Name, string? Description, TicketPriority DefaultPriority);
