using FcgNotifications.Application;
using FcgNotifications.Domain;
using FcgNotifications.Domain.Repositories;
using FcgNotifications.Infrastructure.Database;
using FcgNotifications.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FcgNotifications.IoC;

public static class WorkerServiceCollectionExtensions
{
    public static void ConfigureWorkerDependencies(this IServiceCollection services, IConfiguration configuration)
    {
        // Necessário aqui porque quem chama isso (FcgNotifications.Function) monta o
        // ServiceProvider na mão, sem um Host genérico por trás - sem isso o MediatR
        // quebra ao tentar resolver ILoggerFactory ("MediatR requires ILoggerFactory
        // to be registered. Call services.AddLogging() before services.AddMediatR().").
        services.AddLogging();

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(
            typeof(IDomainEntryPoint).Assembly,
            typeof(IApplicationAssembly).Assembly));

        services.AddDbContext<FcgNotificationsDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Default"),
            npgsql => npgsql.EnableRetryOnFailure()));

        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
    }
}
