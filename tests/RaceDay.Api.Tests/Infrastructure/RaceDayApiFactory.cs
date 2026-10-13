using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using RaceDay.Api.Data;

namespace RaceDay.Api.Tests.Infrastructure;

/// <summary>
/// Runs the real API in memory against its own SQL Server database. Each test class gets a fresh database,
/// which is dropped when the class finishes, so tests never share data.
/// The server comes from the RACEDAY_TEST_CONNECTION environment variable and defaults to local Windows authentication.
/// </summary>
public sealed class RaceDayApiFactory : WebApplicationFactory<Program>
{
    private const string DefaultConnectionString = "Server=localhost;Trusted_Connection=True;TrustServerCertificate=True;";

    private static readonly string BaseConnectionString =
        string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("RACEDAY_TEST_CONNECTION"))
            ? DefaultConnectionString
            : Environment.GetEnvironmentVariable("RACEDAY_TEST_CONNECTION")!;

    private readonly string _databaseName = $"RaceDayTests_{Guid.NewGuid():N}";

    public RaceDayApiFactory()
    {
        WaitForSqlServer();
    }

    private string DatabaseConnectionString =>
        new SqlConnectionStringBuilder(BaseConnectionString) { InitialCatalog = _databaseName }.ConnectionString;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // "Testing" skips HTTPS redirection and the Development-only sample data.
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:DefaultConnection", DatabaseConnectionString);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && Server is not null)
        {
            using var scope = Services.CreateScope();
            scope.ServiceProvider.GetRequiredService<RaceDayDbContext>().Database.EnsureDeleted();
        }

        base.Dispose(disposing);
    }

    /// <summary>A SQL Server container can take a while to accept logins, so retry for up to a minute.</summary>
    private static void WaitForSqlServer()
    {
        var masterConnection = new SqlConnectionStringBuilder(BaseConnectionString) { InitialCatalog = "master" }.ConnectionString;
        Exception? lastError = null;

        for (var attempt = 0; attempt < 30; attempt++)
        {
            try
            {
                using var connection = new SqlConnection(masterConnection);
                connection.Open();
                return;
            }
            catch (SqlException ex)
            {
                lastError = ex;
                Thread.Sleep(TimeSpan.FromSeconds(2));
            }
        }

        throw new InvalidOperationException("SQL Server did not accept connections in time. Check RACEDAY_TEST_CONNECTION.", lastError);
    }
}
