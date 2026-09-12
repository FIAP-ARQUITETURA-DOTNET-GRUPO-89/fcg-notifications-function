using FcgNotifications.Application.Commands;
using FcgNotifications.Domain.Entities;
using FcgNotifications.Domain.Repositories;
using FcgUsers.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;
using OperationResult;

namespace FcgNotifications.Application.Handlers;

public sealed class CreateNotificationHandler(
    INotificationRepository repository,
    ILogger<CreateNotificationHandler> logger)
: IRequestHandler<CreateNotificationCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(CreateNotificationCommand command, CancellationToken cancellationToken)
    {
        var notification = new Notification(command.UserId, Email.Create(command.UserEmail), command.Message, command.Type);

        repository.Add(notification);
        await repository.SaveChangesAsync();

        logger.LogInformation("Notificação criada para o usuário {UserId}", command.UserId);

        return Result.Success(true);
    }
}
