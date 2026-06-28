using DeskFlow.Application.Contracts;
using DeskFlow.Infrastructure.Audit;
using DeskFlow.Infrastructure.Email;
using DeskFlow.Infrastructure.FileStorage;
using DeskFlow.Infrastructure.Identity;
using DeskFlow.Infrastructure.Outbox;
using DeskFlow.Infrastructure.Persistence;
using DeskFlow.Infrastructure.Tickets;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DeskFlow.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sql => sql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));

        services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
        {
            options.Password.RequiredLength = 12;
            options.Password.RequireUppercase = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireDigit = true;
            options.Password.RequireNonAlphanumeric = true;

            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.AllowedForNewUsers = true;

            options.User.RequireUniqueEmail = true;
            options.SignIn.RequireConfirmedEmail = true;
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

        services.AddScoped<ITicketNumberGenerator, TicketNumberGenerator>();
        services.AddScoped<IFileStorage, LocalFileStorage>();
        services.AddScoped<IEmailSender, SmtpEmailSender>();
        services.AddScoped<IAuditLogger, AuditLogService>();
        services.AddScoped<DatabaseSeeder>();

        services.AddHostedService<OutboxWorker>();

        return services;
    }
}
