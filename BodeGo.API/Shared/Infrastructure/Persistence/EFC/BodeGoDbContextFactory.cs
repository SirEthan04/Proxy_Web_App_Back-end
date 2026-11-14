using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using BodeGo.API.Shared.Infrastructure.Hosting;

namespace BodeGo.API.Shared.Infrastructure.Persistence.EFC;

// Design-time model generation does not require access to a database.
public class BodeGoDbContextFactory : IDesignTimeDbContextFactory<BodeGoDbContext>
{
    public BodeGoDbContext CreateDbContext(string[] args)
    {
        var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
            ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("BodeGo.API/appsettings.json", optional: true);
        if (environment == "Development")
            configuration.AddUserSecrets<BodeGoDbContext>(optional: true);
        configuration.AddEnvironmentVariables();
        var settings = configuration.Build();
        var connection = settings.GetConnectionString("DefaultConnection");
        if (environment != "Development")
            connection = BodeGoHosting.DatabaseConnection(settings, isDevelopment: false);
        else if (string.IsNullOrWhiteSpace(connection))
            connection = "Server=localhost;Port=3306;Database=bodego_db;User=root;";
        return new BodeGoDbContext(new DbContextOptionsBuilder<BodeGoDbContext>()
            .UseMySQL(connection).Options);
    }
}
