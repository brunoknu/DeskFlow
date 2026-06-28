using DeskFlow.Api.Policies;
using DeskFlow.Domain.Entities;
using DeskFlow.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DeskFlow.Api.Controllers;

[ApiController]
[Route("api/departments")]
[Authorize]
public class DepartmentsController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public DepartmentsController(ApplicationDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var departments = await _db.Departments
            .Where(d => d.IsActive)
            .Select(d => new { d.Id, d.Name, d.Description })
            .ToListAsync(ct);
        return Ok(departments);
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.CanManageDepartments)]
    public async Task<IActionResult> Create([FromBody] DepartmentRequest req, CancellationToken ct)
    {
        var dept = Department.Create(req.Name, req.Description);
        _db.Departments.Add(dept);
        await _db.SaveChangesAsync(ct);
        return Created($"/api/departments/{dept.Id}", new { id = dept.Id });
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.CanManageDepartments)]
    public async Task<IActionResult> Update(Guid id, [FromBody] DepartmentRequest req, CancellationToken ct)
    {
        var dept = await _db.Departments.FindAsync([id], ct);
        if (dept is null) return NotFound();
        dept.Update(req.Name, req.Description);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}

public sealed record DepartmentRequest(string Name, string? Description);
