namespace DeskFlow.Application.Contracts;

public interface ITicketNumberGenerator
{
    Task<string> GenerateAsync(CancellationToken ct);
}
