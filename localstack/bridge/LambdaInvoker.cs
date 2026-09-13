using System.Text;
using System.Text.Json;
using Amazon.Lambda;
using Amazon.Lambda.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FcgNotifications.Bridge;

public sealed class LambdaInvoker(
    IAmazonLambda lambdaClient,
    IOptions<BridgeSettings> settings,
    ILogger<LambdaInvoker> logger) : ILambdaInvoker
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private const string Vhost = "/";

    public async Task InvokeAsync<TMessage>(string queueName, TMessage message, CancellationToken cancellationToken)
    {
        var messageJson = JsonSerializer.SerializeToUtf8Bytes(message, JsonOptions);
        var messageBase64 = Convert.ToBase64String(messageJson);

        // Mesmo formato que o RabbitMQEvent (Amazon.Lambda.MQEvents) espera receber -
        // ver FcgNotifications.Function/Function.cs.
        var lambdaEvent = new
        {
            eventSource = "aws:rmq",
            eventSourceArn = "arn:aws:mq:us-east-1:000000000000:broker:fcg-notifications-rabbitmq:local-bridge",
            rmqMessagesByQueue = new Dictionary<string, object>
            {
                [$"{queueName}::{Vhost}"] =
                new[]
                {
                    new { redelivered = false, data = messageBase64 },
                },
            },
        };

        var payload = JsonSerializer.Serialize(lambdaEvent, JsonOptions);

        logger.LogInformation(
            "Mensagem recebida em '{QueueName}' - invocando {FunctionName}",
            queueName,
            settings.Value.LambdaFunctionName);

        var response = await lambdaClient.InvokeAsync(
            new InvokeRequest
            {
                FunctionName = settings.Value.LambdaFunctionName,
                InvocationType = InvocationType.RequestResponse,
                Payload = payload,
            },
            cancellationToken);

        // O FunctionHandler daqui só processa o RabbitMQEvent e não devolve valor (retorna
        // "Task", não "Task<T>") - pra esse tipo de handler o LocalStack devolve a invocação
        // sem corpo nenhum, e o SDK deixa "response.Payload" null (não um stream vazio).
        // Sem essa checagem, o StreamReader abaixo lançava ArgumentNullException toda vez
        // que a Lambda respondia OK.
        var responseBody = string.Empty;
        if (response.Payload is not null)
        {
            using var reader = new StreamReader(response.Payload, Encoding.UTF8);
            responseBody = await reader.ReadToEndAsync(cancellationToken);
        }

        if (!string.IsNullOrEmpty(response.FunctionError))
        {
            // Lança pra cima - o MassTransit trata isso como falha na mensagem e faz
            // retry/requeue sozinho (ex.: a function ainda não foi criada no LocalStack).
            throw new InvalidOperationException(
                $"Lambda retornou erro ({response.FunctionError}): {responseBody}");
        }

        logger.LogInformation(
            "Lambda respondeu OK: {ResponseBody}",
            string.IsNullOrEmpty(responseBody) ? "(sem payload de retorno)" : responseBody);
    }
}
