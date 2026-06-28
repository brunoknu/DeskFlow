using DeskFlow.Domain.Enums;

namespace DeskFlow.Api.Policies;

public static class AuthorizationPolicies
{
    public const string CanManageTickets    = nameof(CanManageTickets);
    public const string CanAssignTickets    = nameof(CanAssignTickets);
    public const string CanManageUsers      = nameof(CanManageUsers);
    public const string CanManageDepartments= nameof(CanManageDepartments);
    public const string CanManageSla        = nameof(CanManageSla);
    public const string CanViewReports      = nameof(CanViewReports);
    public const string CanViewInternalNotes= nameof(CanViewInternalNotes);
    public const string CanReopenTicket     = nameof(CanReopenTicket);
    public const string CanViewAuditLogs    = nameof(CanViewAuditLogs);

    public static void Configure(Microsoft.AspNetCore.Authorization.AuthorizationOptions opts)
    {
        opts.AddPolicy(CanManageTickets, p =>
            p.RequireRole(UserRole.Agent, UserRole.Manager, UserRole.Administrator));

        opts.AddPolicy(CanAssignTickets, p =>
            p.RequireRole(UserRole.Agent, UserRole.Manager, UserRole.Administrator));

        opts.AddPolicy(CanManageUsers, p =>
            p.RequireRole(UserRole.Administrator));

        opts.AddPolicy(CanManageDepartments, p =>
            p.RequireRole(UserRole.Manager, UserRole.Administrator));

        opts.AddPolicy(CanManageSla, p =>
            p.RequireRole(UserRole.Manager, UserRole.Administrator));

        opts.AddPolicy(CanViewReports, p =>
            p.RequireRole(UserRole.Manager, UserRole.Administrator));

        opts.AddPolicy(CanViewInternalNotes, p =>
            p.RequireRole(UserRole.Agent, UserRole.Manager, UserRole.Administrator));

        opts.AddPolicy(CanReopenTicket, p =>
            p.RequireAuthenticatedUser());

        opts.AddPolicy(CanViewAuditLogs, p =>
            p.RequireRole(UserRole.Manager, UserRole.Administrator));
    }
}
