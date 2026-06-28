using Microsoft.AspNetCore.Identity;

namespace DeskFlow.Infrastructure.Identity;

public class ApplicationUser : IdentityUser<Guid>
{
    public string FullName { get; set; } = string.Empty;
    public Guid? DepartmentId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset? LastLoginAtUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}
