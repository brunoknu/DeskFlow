namespace DeskFlow.Application.Contracts;

public interface IUserService
{
    Task<bool> IsActiveAsync(Guid userId, CancellationToken ct = default);
    Task<bool> IsAgentOrManagerAsync(Guid userId, CancellationToken ct = default);
}
