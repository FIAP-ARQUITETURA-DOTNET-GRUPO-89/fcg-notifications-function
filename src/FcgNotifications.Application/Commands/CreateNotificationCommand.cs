using FcgNotifications.Domain.Enums;
using MediatR;
using OperationResult;

namespace FcgNotifications.Application.Commands;

public record CreateNotificationCommand(Guid UserId, string UserEmail, string Message, NotificationType Type) : IRequest<Result<bool>>;
