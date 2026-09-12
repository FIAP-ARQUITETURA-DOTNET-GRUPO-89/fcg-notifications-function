using System.Text;
using System.Text.Json;
using Amazon.Lambda.Core;
using Amazon.Lambda.MQEvents;
using FcgNotifications.Application.Commands;
using FcgNotifications.Domain.Enums;
using FcgNotifications.Function.Extensions;
using FgcGames.EventContracts.Events;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace FcgNotifications.Function;

public class Function
{
    // Construído uma única vez por ambiente de execução do Lambda (cold start) e reaproveitado
    // entre invocações que caem no mesmo container, evitando reconstruir o DI a cada mensagem.
    private static readonly IServiceProvider ServiceProvider = BuildServiceProvider();

    // PropertyNameCaseInsensitive porque o corpo real da mensagem (dentro do envelope do
    // MassTransit, ver UnwrapMassTransitEnvelope abaixo) vem em camelCase.
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private static IServiceProvider BuildServiceProvider()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var services = new ServiceCollection();
        services.ConfigureServices(configuration);

        return services.BuildServiceProvider();
    }

    /// <summary>
    /// Handler acionado pelo Amazon MQ (RabbitMQ) event source mapping do Lambda.
    /// O Amazon MQ entrega as mensagens já em lote, agrupadas por fila em RmqMessagesByQueue,
    /// então este handler processa tanto "notifications-payment-processed-events" quanto
    /// "notifications-user-created-events" (as mesmas filas que o Worker consumia via MassTransit).
    /// </summary>
    public async Task FunctionHandler(RabbitMQEvent rabbitMqEvent, ILambdaContext context)
    {
        using var scope = ServiceProvider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        foreach (var (queueKey, messages) in rabbitMqEvent.RmqMessagesByQueue)
        {
            // A chave vem no formato "nomeDaFila::/vhost".
            var queueName = queueKey.Split("::")[0];

            foreach (var message in messages)
            {
                // message.Data vem como string em base64; precisa decodificar antes de virar texto.
                var rawBody = Encoding.UTF8.GetString(Convert.FromBase64String(message.Data));

                // O Users-API/Payments-API publicam pelo MassTransit (publishEndpoint.Publish),
                // que não manda o objeto puro pra fila - ele embrulha num envelope próprio
                // ({ "messageId": ..., "messageType": [...], "message": { ... o evento de
                // verdade, em camelCase ... } }). O Amazon MQ entrega essa mensagem crua pro
                // Lambda (sem o MassTransit por perto pra desembrulhar), então precisamos fazer
                // isso na mão aqui - senão os campos do evento chegam todos nulos/vazios.
                var json = UnwrapMassTransitEnvelope(rawBody);

                switch (queueName)
                {
                    case "notifications-payment-processed-events":
                        await HandlePaymentProcessedAsync(mediator, json, context);
                        break;

                    case "notifications-user-created-events":
                        await HandleUserCreatedAsync(mediator, json, context);
                        break;

                    default:
                        context.Logger.LogWarning($"Mensagem recebida de fila não mapeada: {queueName}");
                        break;
                }
            }
        }
    }

    /// <summary>
    /// Extrai o conteúdo de verdade de dentro do envelope do MassTransit (campo "message"),
    /// se a mensagem vier embrulhada assim. Se não vier (ex.: uma invocação manual de teste
    /// com o payload já "cru", como em ../localstack/events/*.json), usa o corpo como está.
    /// </summary>
    private static string UnwrapMassTransitEnvelope(string rawBody)
    {
        using var document = JsonDocument.Parse(rawBody);

        if (document.RootElement.ValueKind == JsonValueKind.Object &&
            document.RootElement.TryGetProperty("message", out var messageElement))
        {
            return messageElement.GetRawText();
        }

        return rawBody;
    }

    private static async Task HandlePaymentProcessedAsync(IMediator mediator, string json, ILambdaContext context)
    {
        var paymentEvent = JsonSerializer.Deserialize<PaymentProcessedEvent>(json, JsonOptions)
            ?? throw new InvalidOperationException("Payload de PaymentProcessedEvent inválido ou vazio.");

        context.Logger.LogInformation(
            $"PaymentProcessed recebido. OrderId: {paymentEvent.OrderId}, Status: {paymentEvent.Status}.");

        var command = new ProcessPaymentResultCommand(
            paymentEvent.OrderId,
            paymentEvent.UserId,
            paymentEvent.GameId,
            paymentEvent.Status);

        await mediator.Send(command);
    }

    private static async Task HandleUserCreatedAsync(IMediator mediator, string json, ILambdaContext context)
    {
        var userEvent = JsonSerializer.Deserialize<UserCreatedEvent>(json, JsonOptions)
            ?? throw new InvalidOperationException("Payload de UserCreatedEvent inválido ou vazio.");

        context.Logger.LogInformation($"Processando UserCreatedEvent para UserId: {userEvent.UserId}");

        var createUserCommand = new CreateUserCommand(
            userEvent.UserId,
            userEvent.Name,
            userEvent.Email);

        await mediator.Send(createUserCommand);

        var welcomeMessage = $"Olá {userEvent.Name}, bem-vindo à plataforma FCG Games!";

        var createNotificationCommand = new CreateNotificationCommand(
            userEvent.UserId,
            userEvent.Email,
            welcomeMessage,
            NotificationType.Welcome);

        await mediator.Send(createNotificationCommand);

        context.Logger.LogInformation(
            $"Usuário {userEvent.UserId} registrado e notificação de boas-vindas criada com sucesso.");
    }
}
