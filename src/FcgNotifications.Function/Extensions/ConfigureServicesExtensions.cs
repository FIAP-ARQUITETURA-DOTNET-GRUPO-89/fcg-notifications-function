using FcgNotifications.IoC;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FcgNotifications.Function.Extensions;

public static class ConfigureServicesExtensions
{
    /// <summary>
    /// Registra as dependências necessárias para o Lambda processar as mensagens recebidas via
    /// Amazon MQ (RabbitMQ). Diferente do Worker, aqui não há registro de MassTransit/consumers:
    /// o próprio Amazon MQ event source mapping entrega as mensagens ao handler, então não existe
    /// um bus escutando a fila dentro da function.
    /// </summary>
    public static IServiceCollection ConfigureServices(this IServiceCollection services, IConfiguration configuration)
    {
        // ConfigureWorkerDependencies já registra o MediatR, os repositórios e o FcgNotificationsDbContext
        // (ver FcgNotifications.IoC.WorkerServiceCollectionExtensions), então não é preciso repetir aqui.
        services.ConfigureWorkerDependencies(configuration);

        return services;
    }
}
