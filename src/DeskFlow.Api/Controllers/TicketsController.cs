using DeskFlow.Api.Policies;
using DeskFlow.Application.Features.Tickets.AddAttachment;
using DeskFlow.Application.Features.Tickets.AddInternalNote;
using DeskFlow.Application.Features.Tickets.AddPublicComment;
using DeskFlow.Application.Features.Tickets.AssignTicket;
using DeskFlow.Application.Features.Tickets.CancelTicket;
using DeskFlow.Application.Features.Tickets.ChangeTicketStatus;
using DeskFlow.Application.Features.Tickets.CreateTicket;
using DeskFlow.Application.Features.Tickets.GetAttachment;
using DeskFlow.Application.Features.Tickets.GetTicketById;
using DeskFlow.Application.Features.Tickets.RateTicket;
using DeskFlow.Application.Features.Tickets.ReopenTicket;
using DeskFlow.Application.Features.Tickets.ResolveTicket;
using DeskFlow.Application.Features.Tickets.SearchTickets;
using DeskFlow.Domain.Enums;
using DeskFlow.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;

namespace DeskFlow.Api.Controllers;

[ApiController]
[Route("api/tickets")]
[Authorize]
public class TicketsController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;

    public TicketsController(UserManager<ApplicationUser> userManager)
        => _userManager = userManager;

    private async Task<(Guid UserId, bool IsPrivileged, bool IsAgent)> GetUserContextAsync()
    {
        var user = await _userManager.GetUserAsync(User) ?? throw new UnauthorizedAccessException();
        var roles = await _userManager.GetRolesAsync(user);
        var isPrivileged = roles.Any(r => r is UserRole.Agent or UserRole.Manager or UserRole.Administrator);
        var isAgent = roles.Any(r => r is UserRole.Agent or UserRole.Manager);
        return (user.Id, isPrivileged, isAgent);
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateTicketRequest req,
        [FromServices] CreateTicketHandler handler,
        CancellationToken ct)
    {
        var (userId, _, _) = await GetUserContextAsync();
        var cmd = new CreateTicketCommand(req.Title, req.Description, req.DepartmentId,
            req.CategoryId, req.Priority, req.CriticalJustification, userId);
        var result = await handler.HandleAsync(cmd, ct);
        if (!result.IsSuccess) return UnprocessableEntity(new { message = result.Error });
        return CreatedAtAction(nameof(GetById), new { id = result.Value }, new { id = result.Value });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(
        Guid id,
        [FromServices] GetTicketByIdHandler handler,
        CancellationToken ct)
    {
        var (userId, isPrivileged, _) = await GetUserContextAsync();
        var result = await handler.HandleAsync(new GetTicketByIdQuery(id, userId, isPrivileged), ct);
        if (!result.IsSuccess) return NotFound(new { message = result.Error });
        return Ok(result.Value);
    }

    [HttpGet]
    public async Task<IActionResult> Search(
        [FromQuery] SearchTicketsQueryParams qp,
        [FromServices] SearchTicketsHandler handler,
        CancellationToken ct)
    {
        var (userId, isPrivileged, _) = await GetUserContextAsync();
        var query = new SearchTicketsQuery(
            userId, isPrivileged, qp.Status, qp.Priority, qp.AssignedAgentId,
            qp.CategoryId, qp.DepartmentId, qp.Search, qp.IsOverdue,
            qp.SortBy ?? "CreatedAtUtc", qp.SortDescending ?? true,
            qp.Page ?? 1, qp.PageSize ?? 20);
        var result = await handler.HandleAsync(query, ct);
        if (!result.IsSuccess) return BadRequest(new { message = result.Error });
        return Ok(result.Value);
    }

    [HttpPost("{id:guid}/assign")]
    [Authorize(Policy = AuthorizationPolicies.CanAssignTickets)]
    public async Task<IActionResult> Assign(
        Guid id,
        [FromBody] AssignTicketRequest req,
        [FromServices] AssignTicketHandler handler,
        CancellationToken ct)
    {
        var (userId, _, _) = await GetUserContextAsync();
        var result = await handler.HandleAsync(
            new AssignTicketCommand(id, req.AgentId, userId, req.Reason, req.RowVersion), ct);
        return result.IsSuccess ? NoContent() : Conflict(new { message = result.Error });
    }

    [HttpPost("{id:guid}/status")]
    [Authorize(Policy = AuthorizationPolicies.CanManageTickets)]
    public async Task<IActionResult> ChangeStatus(
        Guid id,
        [FromBody] ChangeStatusRequest req,
        [FromServices] ChangeTicketStatusHandler handler,
        CancellationToken ct)
    {
        var (userId, _, _) = await GetUserContextAsync();
        var result = await handler.HandleAsync(
            new ChangeTicketStatusCommand(id, req.NewStatus, userId, req.Reason, req.RowVersion), ct);
        if (!result.IsSuccess)
        {
            if (result.Error!.Contains("modified by another user")) return Conflict(new { message = result.Error });
            return UnprocessableEntity(new { message = result.Error });
        }
        return NoContent();
    }

    [HttpPost("{id:guid}/resolve")]
    [Authorize(Policy = AuthorizationPolicies.CanManageTickets)]
    public async Task<IActionResult> Resolve(
        Guid id,
        [FromBody] ResolveTicketRequest req,
        [FromServices] ResolveTicketHandler handler,
        CancellationToken ct)
    {
        var (userId, _, _) = await GetUserContextAsync();
        var result = await handler.HandleAsync(
            new ResolveTicketCommand(id, req.ResolutionSummary, userId, req.RowVersion), ct);
        if (!result.IsSuccess)
        {
            if (result.Error!.Contains("modified by another user")) return Conflict(new { message = result.Error });
            return UnprocessableEntity(new { message = result.Error });
        }
        return NoContent();
    }

    [HttpPost("{id:guid}/reopen")]
    public async Task<IActionResult> Reopen(
        Guid id,
        [FromBody] ReopenTicketRequest req,
        [FromServices] ReopenTicketHandler handler,
        CancellationToken ct)
    {
        var (userId, _, _) = await GetUserContextAsync();
        var result = await handler.HandleAsync(new ReopenTicketCommand(id, userId, req.RowVersion), ct);
        if (!result.IsSuccess)
        {
            if (result.Error!.Contains("modified by another user")) return Conflict(new { message = result.Error });
            return UnprocessableEntity(new { message = result.Error });
        }
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Cancel(
        Guid id,
        [FromBody] CancelTicketRequest req,
        [FromServices] CancelTicketHandler handler,
        CancellationToken ct)
    {
        var (userId, isPrivileged, _) = await GetUserContextAsync();
        var result = await handler.HandleAsync(
            new CancelTicketCommand(id, userId, isPrivileged, req.Reason, req.RowVersion), ct);
        if (!result.IsSuccess)
        {
            if (result.Error!.Contains("modified by another user")) return Conflict(new { message = result.Error });
            return UnprocessableEntity(new { message = result.Error });
        }
        return NoContent();
    }

    [HttpPost("{id:guid}/comments")]
    public async Task<IActionResult> AddComment(
        Guid id,
        [FromBody] AddCommentRequest req,
        [FromServices] AddPublicCommentHandler handler,
        CancellationToken ct)
    {
        var (userId, _, isAgent) = await GetUserContextAsync();
        var result = await handler.HandleAsync(
            new AddPublicCommentCommand(id, userId, req.Content, isAgent), ct);
        if (!result.IsSuccess) return UnprocessableEntity(new { message = result.Error });
        return Created($"/api/tickets/{id}/comments/{result.Value}", new { id = result.Value });
    }

    [HttpPost("{id:guid}/internal-notes")]
    [Authorize(Policy = AuthorizationPolicies.CanViewInternalNotes)]
    public async Task<IActionResult> AddInternalNote(
        Guid id,
        [FromBody] AddCommentRequest req,
        [FromServices] AddInternalNoteHandler handler,
        CancellationToken ct)
    {
        var (userId, _, _) = await GetUserContextAsync();
        var result = await handler.HandleAsync(new AddInternalNoteCommand(id, userId, req.Content), ct);
        if (!result.IsSuccess) return UnprocessableEntity(new { message = result.Error });
        return Created($"/api/tickets/{id}/internal-notes/{result.Value}", new { id = result.Value });
    }

    [HttpPost("{id:guid}/attachments")]
    public async Task<IActionResult> AddAttachment(
        Guid id,
        IFormFile file,
        [FromServices] AddAttachmentHandler handler,
        CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { message = "No file provided." });

        var (userId, _, _) = await GetUserContextAsync();
        using var stream = file.OpenReadStream();
        var result = await handler.HandleAsync(new AddAttachmentCommand(
            id, userId, file.FileName, file.ContentType, file.Length, stream), ct);

        if (!result.IsSuccess) return UnprocessableEntity(new { message = result.Error });
        return Created($"/api/tickets/{id}/attachments/{result.Value}", new { id = result.Value });
    }

    [HttpGet("{id:guid}/attachments/{attachmentId:guid}")]
    public async Task<IActionResult> DownloadAttachment(
        Guid id,
        Guid attachmentId,
        [FromServices] GetAttachmentHandler handler,
        CancellationToken ct)
    {
        var (userId, isPrivileged, _) = await GetUserContextAsync();
        var result = await handler.HandleAsync(
            new GetAttachmentQuery(attachmentId, id, userId, isPrivileged), ct);

        if (!result.IsSuccess) return NotFound(new { message = result.Error });

        var download = result.Value!;
        // Sanitiza o nome do arquivo para evitar injeção de cabeçalho HTTP.
        var safeFileName = System.Net.WebUtility.UrlEncode(download.OriginalFileName);
        Response.Headers.ContentDisposition = $"attachment; filename*=UTF-8''{safeFileName}";
        return File(download.Content, download.ContentType);
    }

    [HttpPost("{id:guid}/rating")]
    public async Task<IActionResult> Rate(
        Guid id,
        [FromBody] RateTicketRequest req,
        [FromServices] RateTicketHandler handler,
        CancellationToken ct)
    {
        var (userId, _, _) = await GetUserContextAsync();
        var result = await handler.HandleAsync(new RateTicketCommand(id, userId, req.Score, req.Comment), ct);
        if (!result.IsSuccess) return UnprocessableEntity(new { message = result.Error });
        return NoContent();
    }
}

// DTOs de entrada — um por operação para evitar mass assignment.
public sealed record CreateTicketRequest(
    string Title, string Description, Guid DepartmentId, Guid CategoryId,
    TicketPriority Priority, string? CriticalJustification);

public sealed record AssignTicketRequest(Guid? AgentId, string? Reason, byte[] RowVersion);
public sealed record ChangeStatusRequest(TicketStatus NewStatus, string? Reason, byte[] RowVersion);
public sealed record ResolveTicketRequest(string ResolutionSummary, byte[] RowVersion);
public sealed record ReopenTicketRequest(byte[] RowVersion);
public sealed record CancelTicketRequest(string? Reason, byte[] RowVersion);
public sealed record AddCommentRequest(string Content);
public sealed record RateTicketRequest(int Score, string? Comment);

public sealed record SearchTicketsQueryParams(
    TicketStatus? Status, TicketPriority? Priority, Guid? AssignedAgentId,
    Guid? CategoryId, Guid? DepartmentId, string? Search, bool? IsOverdue,
    string? SortBy, bool? SortDescending, int? Page, int? PageSize);
