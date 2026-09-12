using FcgNotifications.Domain.Entities;
using FcgNotifications.Domain.Repositories;
using FcgNotifications.Domain.Enums;
using FcgNotifications.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace FcgNotifications.Infrastructure.Repositories;

public sealed class NotificationRepository(FcgNotificationsDbContext context) : INotificationRepository
{
    public void Add(Notification notification)
        => context.Notifications.Add(notification);

    public Task<Notification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => context.Notifications
            .FirstOrDefaultAsync(
                n => n.Id == id,
                cancellationToken);

    public async Task<IReadOnlyCollection<Notification>> GetPendingAsync(CancellationToken cancellationToken = default)
        => await context.Notifications
            .AsNoTracking()
            .Where(n => n.Status == NotificationStatus.Pending)
            .OrderBy(n => n.CreatedAt)
            .ToListAsync(cancellationToken);

    public Task<int> SaveChangesAsync()
        => context.SaveChangesAsync();
}
