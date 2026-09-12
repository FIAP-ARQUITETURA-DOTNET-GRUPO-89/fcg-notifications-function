using FgcGames.EventContracts.Enums;
using MediatR;
using OperationResult;

namespace FcgNotifications.Application.Commands;

public record ProcessPaymentResultCommand(Guid OrderId, Guid UserId, Guid GameId, PaymentStatus Status) : IRequest<Result<bool>>;
