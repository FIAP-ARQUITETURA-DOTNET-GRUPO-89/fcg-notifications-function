using FgcGames.EventContracts.Events;
using MassTransit;

namespace FcgNotifications.Bridge.Consumers;

public sealed class UserCreatedConsumer(ILambdaInvoker lambdaInvoker) : IConsumer<UserCreatedEvent>
{
    public Task Consume(ConsumeContext<UserCreatedEvent> context) =>
        lambdaInvoker.InvokeAsync("notifications-user-created-events", context.Message, context.CancellationToken);
}
