using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Security.Claims;
using Volo.Abp.Users;

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

        return new PibesDelDestinoDbContext(builder.Options, new NullCurrentUser());
    }

    private static IConfigurationRoot BuildConfiguration()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "../PibesDelDestino.DbMigrator/"))
            .AddJsonFile("appsettings.json", optional: false);

        return builder.Build();
    }

}
    public class NullCurrentUser : ICurrentUser
    {
        public bool IsAuthenticated => false;
        public Guid? Id => null;
        public string? UserName => null;
        public string? Name => null;
        public string? SurName => null;
        public string? Email => null;
        public bool EmailVerified => false;
        public string? PhoneNumber => null;
        public bool PhoneNumberVerified => false;
        public string?[] Roles => new string[0];
        public Claim? FindClaim(string claimType) => null;
        public Claim[] FindClaims(string claimType) => new Claim[0];
        public Claim[] GetAllClaims() => new Claim[0];
        public bool IsInRole(string roleName) => false;
        public Guid? TenantId => null;
    }


