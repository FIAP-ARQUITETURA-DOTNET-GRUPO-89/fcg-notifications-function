using FgcGames.EventContracts.Events;
using MassTransit;

namespace FcgNotifications.Bridge.Consumers;

public sealed class PaymentProcessedConsumer(ILambdaInvoker lambdaInvoker) : IConsumer<PaymentProcessedEvent>
{
    public Task Consume(ConsumeContext<PaymentProcessedEvent> context) =>
        lambdaInvoker.InvokeAsync("notifications-payment-processed-events", context.Message, context.CancellationToken);
}
