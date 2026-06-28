using DeskFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeskFlow.Infrastructure.Persistence.Configurations;

public class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        builder.HasKey(d => d.Id);

        builder.Property(d => d.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(d => d.Description)
            .HasMaxLength(500);

        builder.Property(d => d.IsActive).IsRequired();
        builder.Property(d => d.CreatedAtUtc).IsRequired();
        builder.Property(d => d.UpdatedAtUtc).IsRequired();

        builder.HasIndex(d => d.Name).IsUnique();
        builder.HasIndex(d => d.IsActive);
    }
}
