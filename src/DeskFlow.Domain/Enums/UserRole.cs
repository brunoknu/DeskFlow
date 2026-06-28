namespace DeskFlow.Domain.Enums;

public static class UserRole
{
    public const string Requester = "Requester";
    public const string Agent = "Agent";
    public const string Manager = "Manager";
    public const string Administrator = "Administrator";

    public static readonly IReadOnlyList<string> All =
        [Requester, Agent, Manager, Administrator];
}
