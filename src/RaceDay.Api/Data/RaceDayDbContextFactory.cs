using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace RaceDay.Api.Data;

/// <summary>
/// Lets <c>dotnet ef migrations add</c> create the context without starting the web host,
/// so design-time commands never try to migrate or seed a live database.
/// It reads the same connection string as the running API, so the environment variable
/// ConnectionStrings__DefaultConnection applies to both.
/// </summary>
public class RaceDayDbContextFactory : IDesignTimeDbContextFactory<RaceDayDbContext>
{
    public RaceDayDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var options = new DbContextOptionsBuilder<RaceDayDbContext>()
            .UseSqlServer(configuration.GetConnectionString("DefaultConnection"))
            .Options;

        return new RaceDayDbContext(options);
    }
}
