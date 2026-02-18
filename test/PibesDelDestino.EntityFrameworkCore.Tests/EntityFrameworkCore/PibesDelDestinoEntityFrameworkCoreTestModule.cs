using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Security.Claims;
using Volo.Abp;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore.Sqlite;
using Volo.Abp.FeatureManagement;
using Volo.Abp.Modularity;
using Volo.Abp.PermissionManagement;
using Volo.Abp.Uow;
using Volo.Abp.Users;

namespace PibesDelDestino.EntityFrameworkCore;

[DependsOn(
    typeof(PibesDelDestinoApplicationTestModule), // 👈 Depende de Aplicación (Estilo compañeros)
    typeof(PibesDelDestinoEntityFrameworkCoreModule),
    typeof(AbpEntityFrameworkCoreSqliteModule)
)]
public class PibesDelDestinoEntityFrameworkCoreTestModule : AbpModule
{
    private SqliteConnection? _sqliteConnection;

    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<FeatureManagementOptions>(options =>
        {
            options.SaveStaticFeaturesToDatabase = false;
            options.IsDynamicFeatureStoreEnabled = false;
        });

        Configure<PermissionManagementOptions>(options =>
        {
            options.SaveStaticPermissionsToDatabase = false;
            options.IsDynamicPermissionStoreEnabled = false;
        });

        context.Services.AddAlwaysDisableUnitOfWorkTransaction();

        ConfigureInMemorySqlite(context.Services);
    }

    private void ConfigureInMemorySqlite(IServiceCollection services)
    {
        _sqliteConnection = CreateDatabaseAndGetConnection();

        services.Configure<AbpDbContextOptions>(options =>
        {
            options.Configure(context =>
            {
                context.DbContextOptions.UseSqlite(_sqliteConnection);
            });
        });
    }

    public override void OnApplicationShutdown(ApplicationShutdownContext context)
    {
        _sqliteConnection?.Dispose();
    }

    private static SqliteConnection CreateDatabaseAndGetConnection()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<PibesDelDestinoDbContext>()
            .UseSqlite(connection)
            .Options;

        // Se usa NullCurrentUser para que no pida un usuario autenticado al crear las tablas
        using (var context = new PibesDelDestinoDbContext(options, new NullCurrentUser()))
        {
            context.GetService<IRelationalDatabaseCreator>().CreateTables();
        }

        return connection;
    }
}

// Clase auxiliar para los tests
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
    public string?[] Roles => Array.Empty<string>();
    public Claim? FindClaim(string claimType) => null;
    public Claim[] FindClaims(string claimType) => Array.Empty<Claim>();
    public Claim[] GetAllClaims() => Array.Empty<Claim>();
    public bool IsInRole(string roleName) => false;
    public Guid? TenantId => null;
}