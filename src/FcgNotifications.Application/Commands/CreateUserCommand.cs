using MediatR;
using OperationResult;

namespace FcgNotifications.Application.Commands;

public record CreateUserCommand(Guid UserId, string Name, string Email) : IRequest<Result<bool>>;
