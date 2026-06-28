using DeskFlow.Domain.Entities;
using DeskFlow.Domain.Enums;
using DeskFlow.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DeskFlow.Infrastructure.Persistence;

public class DatabaseSeeder
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;
    private readonly ILogger<DatabaseSeeder> _logger;
    private readonly TimeProvider _time;

    public DatabaseSeeder(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        ILogger<DatabaseSeeder> logger,
        TimeProvider time)
    {
        _db = db;
        _userManager = userManager;
        _roleManager = roleManager;
        _logger = logger;
        _time = time;
    }

    public async Task SeedAsync()
    {
        await _db.Database.MigrateAsync();
        await SeedRolesAsync();
        await SeedDepartmentsAsync();
        await SeedCategoriesAsync();
        await SeedSlaPoliciesAsync();
        await SeedUsersAsync();
        await SeedTicketsAsync();
        _logger.LogInformation("Development seed completed.");
    }

    private async Task SeedRolesAsync()
    {
        foreach (var role in UserRole.All)
        {
            if (!await _roleManager.RoleExistsAsync(role))
            {
                await _roleManager.CreateAsync(new IdentityRole<Guid>(role) { Id = Guid.NewGuid() });
                _logger.LogInformation("Role '{Role}' created.", role);
            }
        }
    }

    private async Task SeedDepartmentsAsync()
    {
        var departments = new[] { "Tecnologia", "Financeiro", "Recursos Humanos", "Comercial", "Operações" };
        foreach (var name in departments)
        {
            if (!await _db.Departments.AnyAsync(d => d.Name == name))
            {
                _db.Departments.Add(Department.Create(name, $"Departamento de {name}"));
            }
        }
        await _db.SaveChangesAsync();
    }

    private async Task SeedCategoriesAsync()
    {
        var categories = new[]
        {
            ("Acesso e permissões", TicketPriority.High),
            ("Equipamentos", TicketPriority.Medium),
            ("Sistemas internos", TicketPriority.High),
            ("Rede e conectividade", TicketPriority.High),
            ("E-mail", TicketPriority.Medium),
            ("Instalação de software", TicketPriority.Low),
            ("Solicitação de melhoria", TicketPriority.Low),
            ("Outros", TicketPriority.Low)
        };

        foreach (var (name, priority) in categories)
        {
            if (!await _db.TicketCategories.AnyAsync(c => c.Name == name))
                _db.TicketCategories.Add(TicketCategory.Create(name, null, priority));
        }
        await _db.SaveChangesAsync();
    }

    private async Task SeedSlaPoliciesAsync()
    {
        var policies = new[]
        {
            ("SLA Low", TicketPriority.Low, 480, 4320),
            ("SLA Medium", TicketPriority.Medium, 240, 1440),
            ("SLA High", TicketPriority.High, 60, 480),
            ("SLA Critical", TicketPriority.Critical, 15, 240)
        };

        foreach (var (name, priority, firstResponse, resolution) in policies)
        {
            if (!await _db.SlaPolicies.AnyAsync(s => s.Priority == priority && s.IsActive))
                _db.SlaPolicies.Add(SlaPolicy.Create(name, priority, firstResponse, resolution));
        }
        await _db.SaveChangesAsync();
    }

    private async Task SeedUsersAsync()
    {
        var techDept = await _db.Departments.FirstAsync(d => d.Name == "Tecnologia");
        var now = _time.GetUtcNow();

        var users = new[]
        {
            ("admin@deskflow.local",    "Administrator", "Admin DeskFlow",    UserRole.Administrator, techDept.Id),
            ("manager@deskflow.local",  "Manager#2026",  "Carla Mendes",      UserRole.Manager,       techDept.Id),
            ("agent1@deskflow.local",   "Agent#2026!",   "Rafael Souza",      UserRole.Agent,         techDept.Id),
            ("agent2@deskflow.local",   "Agent#2026!",   "Juliana Castro",    UserRole.Agent,         techDept.Id),
            ("requester@deskflow.local","Req#2026!",     "Paulo Ferreira",    UserRole.Requester,     Guid.Empty),
            ("user2@deskflow.local",    "User#2026!",    "Ana Lima",          UserRole.Requester,     Guid.Empty)
        };

        foreach (var (email, password, fullName, role, deptId) in users)
        {
            if (await _userManager.FindByEmailAsync(email) is not null) continue;

            var user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FullName = fullName,
                DepartmentId = deptId == Guid.Empty ? null : deptId,
                IsActive = true,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };

            var result = await _userManager.CreateAsync(user, password);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, role);
                _logger.LogInformation("User '{Email}' created with role '{Role}'.", email, role);
            }
            else
            {
                _logger.LogError("Failed to create user '{Email}': {Errors}", email,
                    string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
    }

    private async Task SeedTicketsAsync()
    {
        if (await _db.Tickets.AnyAsync()) return;

        var requester = await _userManager.FindByEmailAsync("requester@deskflow.local");
        var agent1 = await _userManager.FindByEmailAsync("agent1@deskflow.local");
        if (requester is null || agent1 is null) return;

        var dept = await _db.Departments.FirstAsync(d => d.Name == "Tecnologia");
        var cat = await _db.TicketCategories.FirstAsync(c => c.Name == "Acesso e permissões");
        var sla = await _db.SlaPolicies.FirstAsync(s => s.Priority == TicketPriority.High);
        var now = _time.GetUtcNow();

        var t1 = Ticket.Create(
            "HD-2026-000001",
            "Sem acesso ao sistema de RH",
            "Não consigo acessar o sistema de RH desde segunda-feira. Preciso urgentemente para fechar a folha.",
            requester.Id, dept.Id, cat.Id, TicketPriority.High,
            sla.CalculateFirstResponseDue(now), sla.CalculateResolutionDue(now), now);
        _db.Tickets.Add(t1);

        var t2 = Ticket.Create(
            "HD-2026-000002",
            "Computador não liga",
            "Meu computador não está ligando desde essa manhã.",
            requester.Id, dept.Id,
            (await _db.TicketCategories.FirstAsync(c => c.Name == "Equipamentos")).Id,
            TicketPriority.Medium,
            (await _db.SlaPolicies.FirstAsync(s => s.Priority == TicketPriority.Medium)).CalculateFirstResponseDue(now),
            (await _db.SlaPolicies.FirstAsync(s => s.Priority == TicketPriority.Medium)).CalculateResolutionDue(now),
            now);
        _db.Tickets.Add(t2);

        await _db.SaveChangesAsync();

        // Assign ticket 1 and transition to InProgress
        t1.Transition(TicketStatus.Triaged, agent1.Id, now.AddMinutes(5));
        t1.Assign(agent1.Id, agent1.Id, now.AddMinutes(6));
        t1.Transition(TicketStatus.InProgress, agent1.Id, now.AddMinutes(7));
        t1.AddPublicComment(agent1.Id, "Olá Paulo, estou analisando o seu acesso. Aguarde um momento.", now.AddMinutes(10));
        t1.RecordFirstResponse(now.AddMinutes(10));

        await _db.SaveChangesAsync();
        _logger.LogInformation("Ticket seed completed.");
    }
}
