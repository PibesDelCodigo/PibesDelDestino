using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PibesDelDestino.Data;
using Volo.Abp.DependencyInjection;

namespace PibesDelDestino.EntityFrameworkCore;

public class EntityFrameworkCorePibesDelDestinoDbSchemaMigrator
    : IPibesDelDestinoDbSchemaMigrator, ITransientDependency
{
    private readonly IServiceProvider _serviceProvider;

    public EntityFrameworkCorePibesDelDestinoDbSchemaMigrator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task MigrateAsync()
    {
        /* We intentionally resolving the PibesDelDestinoDbContext
         * from IServiceProvider (instead of directly injecting it)
         * to properly get the connection string of the current tenant in the
         * current scope.
         */

        await _serviceProvider
            .GetRequiredService<PibesDelDestinoDbContext>()
            .Database
            .MigrateAsync();
    }
}
