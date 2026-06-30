using DeskFlow.Application.Contracts;
using DeskFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DeskFlow.Infrastructure.Outbox;

public class OutboxWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxWorker> _logger;
    private readonly TimeProvider _time;
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(30);

    public OutboxWorker(IServiceScopeFactory scopeFactory, ILogger<OutboxWorker> logger, TimeProvider time)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _time = time;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Outbox worker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingMessagesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Outbox worker encountered an error.");
            }

            await Task.Delay(Interval, stoppingToken);
        }
    }

    private async Task ProcessPendingMessagesAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();

        var now = _time.GetUtcNow();

        var messages = await db.OutboxMessages
            .Where(m => m.ProcessedAtUtc == null
                     && m.NextAttemptAtUtc <= now
                     && m.AttemptCount < 5)
            .OrderBy(m => m.OccurredAtUtc)
            .Take(20)
            .ToListAsync(ct);

        foreach (var message in messages)
        {
            try
            {
                await DispatchAsync(message, emailSender, db, ct);
                message.MarkProcessed(_time.GetUtcNow());
                _logger.LogInformation("Outbox message {Id} ({Type}) processed.", message.Id, message.Type);
            }
            catch (Exception ex)
            {
                message.RecordFailure(ex.Message, _time.GetUtcNow());
                _logger.LogWarning("Outbox message {Id} failed (attempt {Attempt}): {Error}",
                    message.Id, message.AttemptCount, ex.Message);
            }
        }

        if (messages.Count > 0)
            await db.SaveChangesAsync(ct);
    }

    private async Task DispatchAsync(
        Domain.Entities.OutboxMessage message,
        IEmailSender emailSender,
        ApplicationDbContext db,
        CancellationToken ct)
    {
        var payload = System.Text.Json.JsonDocument.Parse(message.Payload).RootElement;

        switch (message.Type)
        {
            case "TicketCreated":
            {
                var requesterId = payload.GetProperty("RequesterId").GetGuid();
                var user = await db.Users.FindAsync([requesterId], ct);
                if (user?.Email is not null)
                    await emailSender.SendAsync(user.Email,
                        $"Chamado {payload.GetProperty("Number").GetString()} criado com sucesso",
                        $"<p>Seu chamado <strong>{payload.GetProperty("Number").GetString()}</strong> foi aberto: <em>{payload.GetProperty("Title").GetString()}</em></p>",
                        ct);
                break;
            }
            case "TicketResolved":
            {
                var requesterId = payload.GetProperty("RequesterId").GetGuid();
                var user = await db.Users.FindAsync([requesterId], ct);
                if (user?.Email is not null)
                    await emailSender.SendAsync(user.Email,
                        $"Chamado {payload.GetProperty("Number").GetString()} resolvido",
                        $"<p>Seu chamado <strong>{payload.GetProperty("Number").GetString()}</strong> foi resolvido. Por favor, avalie o atendimento.</p>",
                        ct);
                break;
            }
            case "TicketAssigned":
            {
                var agentId = payload.GetProperty("AgentId").GetGuid();
                var agent = await db.Users.FindAsync([agentId], ct);
                if (agent?.Email is not null)
                    await emailSender.SendAsync(agent.Email,
                        $"Chamado {payload.GetProperty("Number").GetString()} atribuído a você",
                        $"<p>O chamado <strong>{payload.GetProperty("Number").GetString()}</strong> foi atribuído a você.</p>",
                        ct);
                break;
            }
            case "TicketCommentAdded":
            case "TicketReopened":
            case string s when s.StartsWith("TicketStatus_"):
                // Demais tipos de notificação — implementar conforme necessário.
                break;

            default:
                _logger.LogWarning("Unknown outbox message type: {Type}", message.Type);
                break;
        }
    }
}
