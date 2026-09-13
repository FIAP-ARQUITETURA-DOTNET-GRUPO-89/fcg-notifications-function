using Amazon.Lambda;
using Amazon.Runtime;
using FcgNotifications.Bridge;
using FcgNotifications.Bridge.Consumers;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

var builder = Host.CreateApplicationBuilder(args);

// Host.CreateApplicationBuilder já inclui variáveis de ambiente como fonte de
// configuração por padrão - não precisa chamar AddEnvironmentVariables() de novo.
builder.Services.Configure<BridgeSettings>(builder.Configuration.GetSection("Bridge"));

builder.Services.AddSingleton<IAmazonLambda>(sp =>
{
    var settings = sp.GetRequiredService<IOptions<BridgeSettings>>().Value;

    // O LocalStack não valida credenciais de verdade, mas o SDK exige alguma.
    var credentials = new BasicAWSCredentials("test", "test");
    var config = new AmazonLambdaConfig
    {
        ServiceURL = settings.LambdaEndpointUrl,
        AuthenticationRegion = settings.AwsRegion,
    };

    return new AmazonLambdaClient(credentials, config);
});

builder.Services.AddSingleton<ILambdaInvoker, LambdaInvoker>();

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<UserCreatedConsumer>();
    x.AddConsumer<PaymentProcessedConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        var connectionString = context.GetRequiredService<IConfiguration>().GetConnectionString("Rabbitmq")
            ?? throw new InvalidOperationException("ConnectionStrings__Rabbitmq não configurada.");

        cfg.Host(connectionString);

        // Nomes de fila explícitos (em vez de deixar o ConfigureEndpoints gerar um nome
        // pela convenção) pra bater exatamente com o que o Worker antigo usava e com o
        // que o Terraform (../../terraform/lambda.tf) espera na AWS de verdade.
        cfg.ReceiveEndpoint("notifications-user-created-events", e =>
        {
            e.ConfigureConsumer<UserCreatedConsumer>(context);
        });

        cfg.ReceiveEndpoint("notifications-payment-processed-events", e =>
        {
            e.ConfigureConsumer<PaymentProcessedConsumer>(context);
        });
    });
});

var host = builder.Build();
await host.RunAsync();
