using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Jewel.JPMS.Api.Data;

public sealed class JpmsContextDesignTimeFactory : IDesignTimeDbContextFactory<JpmsContext>
{
    public JpmsContext CreateDbContext(string[] args)
    {
        // Honour the same SqlConnectionString setting the app uses, so
        // `dotnet ef database update` can target a real (e.g. Azure) database.
        // Falls back to a local design-time database for offline model work.
        var connectionString = Environment.GetEnvironmentVariable("SqlConnectionString")
            ?? "Server=localhost;Database=JpmsDesignTime;Trusted_Connection=True;";

        var options = new DbContextOptionsBuilder<JpmsContext>()
            .UseSqlServer(connectionString)
            .Options;
        return new JpmsContext(options);
    }
}
