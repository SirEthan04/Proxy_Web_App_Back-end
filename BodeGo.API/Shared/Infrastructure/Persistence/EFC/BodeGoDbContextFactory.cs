using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BodeGo.API.Shared.Infrastructure.Persistence.EFC;

// Design-time model generation does not require access to a database.
public class BodeGoDbContextFactory : IDesignTimeDbContextFactory<BodeGoDbContext>
{
    public BodeGoDbContext CreateDbContext(string[] args)
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("BodeGo.API/appsettings.json", optional: true);
        if (environment == "Development")
            configuration.AddUserSecrets<BodeGoDbContext>(optional: true);
        configuration.AddEnvironmentVariables();
        var connection = configuration.Build().GetConnectionString("DefaultConnection");
        connection = string.IsNullOrWhiteSpace(connection)
            ? "Server=localhost;Port=3306;Database=bodego_db;User=root;"
            : connection;
        return new BodeGoDbContext(new DbContextOptionsBuilder<BodeGoDbContext>()
            .UseMySQL(connection).Options);
    }
}
