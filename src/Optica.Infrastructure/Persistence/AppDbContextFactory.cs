using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Optica.Infrastructure.Persistence;
public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var cfg = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.Development.json", optional:true)
            .AddJsonFile("appsettings.json", optional:true)
            .AddEnvironmentVariables()
            .Build();
        var cs = cfg.GetConnectionString("SqlServer") ?? cfg["SqlServer:ConnectionString"] ??
                 "Server=localhost;Database=OpticaDb;Trusted_Connection=True;TrustServerCertificate=True";
        var opt = new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(cs);
        return new AppDbContext(opt.Options);
    }
}
