using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace PibesDelDestino.EntityFrameworkCore;

/* This class is needed for EF Core console commands
 * (like Add-Migration and Update-Database commands) */
public class PibesDelDestinoDbContextFactory : IDesignTimeDbContextFactory<PibesDelDestinoDbContext>
{
    public PibesDelDestinoDbContext CreateDbContext(string[] args)
    {
        var configuration = BuildConfiguration();
        
        PibesDelDestinoEfCoreEntityExtensionMappings.Configure();

        var builder = new DbContextOptionsBuilder<PibesDelDestinoDbContext>()
            .UseSqlServer(configuration.GetConnectionString("Default"));
        
        return new PibesDelDestinoDbContext(builder.Options);
    }

    private static IConfigurationRoot BuildConfiguration()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "../PibesDelDestino.DbMigrator/"))
            .AddJsonFile("appsettings.json", optional: false);

        return builder.Build();
    }
}
