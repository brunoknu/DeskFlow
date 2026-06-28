using DeskFlow.Application.Contracts;
using DeskFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DeskFlow.Infrastructure.Tickets;

public class TicketNumberGenerator : ITicketNumberGenerator
{
    private readonly ApplicationDbContext _db;

    public TicketNumberGenerator(ApplicationDbContext db) => _db = db;

    public async Task<string> GenerateAsync(CancellationToken ct)
    {
        var year = DateTimeOffset.UtcNow.Year;
        var prefix = $"HD-{year}-";

        var last = await _db.Tickets
            .Where(t => t.Number.StartsWith(prefix))
            .OrderByDescending(t => t.Number)
            .Select(t => t.Number)
            .FirstOrDefaultAsync(ct);

        long next = 1;
        if (last is not null)
        {
            var seq = last[prefix.Length..];
            if (long.TryParse(seq, out var parsed))
                next = parsed + 1;
        }

        return $"{prefix}{next:D6}";
    }
}
