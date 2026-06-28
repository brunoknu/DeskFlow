using DeskFlow.Api.Policies;
using DeskFlow.Domain.Entities;
using DeskFlow.Domain.Enums;
using DeskFlow.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DeskFlow.Api.Controllers;

[ApiController]
[Route("api/sla-policies")]
[Authorize(Policy = AuthorizationPolicies.CanManageSla)]
public class SlaPoliciesController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public SlaPoliciesController(ApplicationDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var policies = await _db.SlaPolicies
            .Select(s => new { s.Id, s.Name, s.Priority, s.FirstResponseMinutes, s.ResolutionMinutes, s.IsActive })
            .ToListAsync(ct);
        return Ok(policies);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] SlaPolicyRequest req, CancellationToken ct)
    {
        var policy = SlaPolicy.Create(req.Name, req.Priority, req.FirstResponseMinutes, req.ResolutionMinutes);
        _db.SlaPolicies.Add(policy);
        await _db.SaveChangesAsync(ct);
        return Created($"/api/sla-policies/{policy.Id}", new { id = policy.Id });
    }
}

public sealed record SlaPolicyRequest(
    string Name, TicketPriority Priority, int FirstResponseMinutes, int ResolutionMinutes);
