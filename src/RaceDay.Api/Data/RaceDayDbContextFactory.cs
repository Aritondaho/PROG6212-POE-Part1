using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace RaceDay.Api.Data;

/// <summary>
/// Lets <c>dotnet ef migrations add</c> create the context without starting the web host,
/// so design-time commands never try to migrate or seed a live database.
/// </summary>
public class RaceDayDbContextFactory : IDesignTimeDbContextFactory<RaceDayDbContext>
{
    public RaceDayDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<RaceDayDbContext>()
            .UseSqlServer("Server=localhost;Database=RaceDayDB;Trusted_Connection=True;TrustServerCertificate=True;")
            .Options;

        return new RaceDayDbContext(options);
    }
}
