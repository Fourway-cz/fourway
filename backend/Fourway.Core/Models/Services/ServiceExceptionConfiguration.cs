using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fourway.Core.Models;

/// <summary>
/// EF Core konfigurace pro <see cref="ServiceException"/>.
/// </summary>
public sealed class ServiceExceptionConfiguration : IEntityTypeConfiguration<ServiceException>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ServiceException> builder)
    {
        builder.ToTable("service_exceptions");

        builder.HasKey(x => new { x.ServiceId, x.Date });

        builder.Property(x => x.ServiceId).IsRequired();
        builder.Property(x => x.Date).IsRequired();
        builder.Property(x => x.ExceptionType).IsRequired();
    }
}
