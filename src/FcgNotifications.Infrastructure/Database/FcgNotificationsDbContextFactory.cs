using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace FcgNotifications.Infrastructure.Database;

public class FcgNotificationsDbContextFactory : IDesignTimeDbContextFactory<FcgNotificationsDbContext>
{
    public FcgNotificationsDbContext CreateDbContext(string[] args)
    {
        // Usado só em design-time (dotnet ef migrations/database update). Como este repo não
        // tem mais um projeto "host" com appsettings.json (o Worker ficou no fcg-notifications),
        // lê a connection string da variável de ambiente ConnectionStrings__Default e cai para
        // um Postgres local (localhost:5432) como default, pra rodar migrations direto da máquina
        // contra o Postgres exposto pelo docker-compose do LocalStack.
        var configuration = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<FcgNotificationsDbContext>();
        var connectionString = configuration.GetConnectionString("Default")
            ?? "Host=localhost;Port=5432;Database=fcgnotifications-db;Username=postgres;Password=postgres";

        optionsBuilder.UseNpgsql(connectionString);

        return new FcgNotificationsDbContext(optionsBuilder.Options);
    }
}
