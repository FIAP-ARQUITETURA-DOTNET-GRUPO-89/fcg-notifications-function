using MassTransit;
using FcgNotifications.SharedKernel.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FcgNotifications.Infrastructure.Messaging;

public static class MassTransitConfiguration
{
    /// <summary>
    /// Configura o MassTransit para consumo e publicação de eventos, incluindo registro de consumers, filas e endpoints do RabbitMQ.
    /// Deve ser utilizado por Workers ou serviços que consumam mensagens.
    /// </summary>
    public static IServiceCollection AddMassTransitRabbitMq(this IServiceCollection services, IConfiguration configuration, Action<IBusRegistrationConfigurator> configureConsumers)
    {
        services.AddMassTransit(x =>
        {
            configureConsumers(x);

            x.UsingRabbitMq((context, cfg) =>
            {
                var connectionString = configuration.GetConnectionString("Rabbitmq");

                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    throw new InvalidOperationException("RabbitMQ connection string não configurada.");
                }

                cfg.Host(connectionString);

                var retry = context.GetRequiredService<IOptions<MassTransitSettings>>().Value;

                cfg.UseMessageRetry(r =>
                {
                    r.Exponential(
                        retryLimit: retry.RetryLimit,
                        minInterval: TimeSpan.FromSeconds(retry.MinIntervalSeconds),
                        maxInterval: TimeSpan.FromSeconds(retry.MaxIntervalSeconds),
                        intervalDelta: TimeSpan.FromSeconds(retry.IntervalDeltaSeconds));
                });

                cfg.ConfigureEndpoints(context);
            });
        });

        return services;
    }

    /// <summary>
    /// Configura o MassTransit apenas para publicação de eventos.
    /// Deve ser utilizado por APIs ou serviços que apenas enviam mensagens para o broker e não possuem consumers.
    /// </summary>
    public static IServiceCollection AddMassTransitRabbitMqPublisher(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMassTransit(x =>
        {
            x.UsingRabbitMq((context, cfg) =>
            {
                var connectionString = configuration.GetConnectionString("Rabbitmq");

                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    throw new InvalidOperationException("RabbitMQ connection string não configurada.");
                }

                cfg.Host(connectionString);
            });
        });

        return services;
    }
}