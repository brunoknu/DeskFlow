using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DeskFlow.Infrastructure.Persistence;

// Usado apenas pelo tooling do EF Core (dotnet ef migrations add). Não registrado no DI.
public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(
                "Server=localhost,1433;Database=DeskFlowDev;User Id=sa;Password=Dev@Passw0rd!;TrustServerCertificate=True;",
                sql => sql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName))
            .Options;

        return new ApplicationDbContext(options);
    }
}
