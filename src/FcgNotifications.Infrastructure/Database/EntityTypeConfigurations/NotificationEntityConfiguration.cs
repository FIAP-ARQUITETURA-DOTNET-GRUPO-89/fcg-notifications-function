using FcgNotifications.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FcgNotifications.Infrastructure.Database.EntityTypeConfigurations.Notifications;

public class NotificationEntityConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.UserId)
            .IsRequired();

        builder.Property(x => x.Message)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(x => x.Type)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.ComplexProperty(p => p.UserEmail, e =>
        {
            e.Property(email => email.Address)
                .HasColumnName("UserEmail")
                .HasMaxLength(255)
                .IsRequired();
        });

        builder.Property(x => x.CreatedAt)
            .IsRequired();
    }
}
