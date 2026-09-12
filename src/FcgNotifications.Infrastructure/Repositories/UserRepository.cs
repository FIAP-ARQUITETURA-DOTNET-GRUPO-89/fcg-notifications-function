using FcgNotifications.Domain.Entities;
using FcgNotifications.Domain.Repositories;
using FcgNotifications.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace FcgNotifications.Infrastructure.Repositories;

public sealed class UserRepository(FcgNotificationsDbContext context): IUserRepository
{
    public async Task<User?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default) 
        => await context.Users
            .FirstOrDefaultAsync(
                x => x.Id == userId,
                cancellationToken);

    public async Task<bool> ExistsAsync(Guid userId, CancellationToken cancellationToken = default) 
        => await context.Users
            .AnyAsync(
                x => x.Id == userId,
                cancellationToken);

    public void Add(User user) => context.Users.Add(user);

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default) 
        => await context.SaveChangesAsync(cancellationToken);
}
