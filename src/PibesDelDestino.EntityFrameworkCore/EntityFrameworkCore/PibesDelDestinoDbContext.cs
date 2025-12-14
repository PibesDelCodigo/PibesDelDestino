using Microsoft.EntityFrameworkCore;
using PibesDelDestino.Destinations;
using PibesDelDestino.Experiences;
using PibesDelDestino.Favorites;
using PibesDelDestino.Ratings;
using PibesDelDestino.Users;
using System;
using System.Linq.Expressions;
using Volo.Abp.AuditLogging.EntityFrameworkCore;
using Volo.Abp.BackgroundJobs.EntityFrameworkCore;
using Volo.Abp.BlobStoring.Database.EntityFrameworkCore;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore.Modeling;
using Volo.Abp.FeatureManagement.EntityFrameworkCore;
using Volo.Abp.Identity;
using Volo.Abp.Identity.EntityFrameworkCore;
using Volo.Abp.OpenIddict.EntityFrameworkCore;
using Volo.Abp.PermissionManagement.EntityFrameworkCore;
using Volo.Abp.SettingManagement.EntityFrameworkCore;
using Volo.Abp.Users;

namespace PibesDelDestino.EntityFrameworkCore;

[ReplaceDbContext(typeof(IIdentityDbContext))]
[ConnectionStringName("Default")]
public class PibesDelDestinoDbContext :
    AbpDbContext<PibesDelDestinoDbContext>,
    IIdentityDbContext
{
    /* Add DbSet properties for your Aggregate Roots / Entities here. */

    public DbSet<FavoriteDestination> FavoriteDestinations { get; set; }
    public DbSet<TravelExperience> TravelExperiences { get; set; }
    public DbSet<Destination> Destinations { get; set; }
    #region Entities from the modules

    /* Notice: We only implemented IIdentityProDbContext 
     * and replaced them for this DbContext. This allows you to perform JOIN
     * queries for the entities of these modules over the repositories easily. You
     * typically don't need that for other modules. But, if you need, you can
     * implement the DbContext interface of the needed module and use ReplaceDbContext
     * attribute just like IIdentityProDbContext .
     *
     * More info: Replacing a DbContext of a module ensures that the related module
     * uses this DbContext on runtime. Otherwise, it will use its own DbContext class.
     */

    // Identity
    public DbSet<IdentityUser> Users { get; set; }
    public DbSet<IdentityRole> Roles { get; set; }
    public DbSet<IdentityClaimType> ClaimTypes { get; set; }
    public DbSet<OrganizationUnit> OrganizationUnits { get; set; }
    public DbSet<IdentitySecurityLog> SecurityLogs { get; set; }
    public DbSet<IdentityLinkUser> LinkUsers { get; set; }
    public DbSet<IdentityUserDelegation> UserDelegations { get; set; }
    public DbSet<IdentitySession> Sessions { get; set; }
    public DbSet<Rating> Ratings { get; set; }

    private readonly ICurrentUser _currentUser;
    #endregion

    public PibesDelDestinoDbContext(
            DbContextOptions<PibesDelDestinoDbContext> options,
            ICurrentUser currentUser)
        : base(options)
    {
        _currentUser = currentUser;
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        /* Include modules to your migration db context */

        builder.ConfigurePermissionManagement();
        builder.ConfigureSettingManagement();
        builder.ConfigureBackgroundJobs();
        builder.ConfigureAuditLogging();
        builder.ConfigureFeatureManagement();
        builder.ConfigureIdentity();
        builder.ConfigureOpenIddict();
        builder.ConfigureBlobStoring();

        builder.Entity<TravelExperience>(b =>
        {
            b.ToTable(PibesDelDestinoConsts.DbTablePrefix + "TravelExperiences", PibesDelDestinoConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.Title).IsRequired().HasMaxLength(100);
            b.Property(x => x.Description).IsRequired().HasMaxLength(4000);
            b.Property(x => x.Rating).IsRequired(); // Obligatorio

            // Índices para que las búsquedas sean rápidas
            b.HasIndex(x => x.DestinationId);
            b.HasIndex(x => x.UserId);
        });

        builder.Entity<FavoriteDestination>(b =>
        {
            b.ToTable(PibesDelDestinoConsts.DbTablePrefix + "FavoriteDestinations", PibesDelDestinoConsts.DbSchema);
            b.ConfigureByConvention();

            // IMPORTANTE: Índice ÚNICO compuesto.
            // Esto impide que la base de datos acepte dos filas con el mismo Usuario + Destino.
            b.HasIndex(x => new { x.UserId, x.DestinationId }).IsUnique();
        });


        builder.Entity<Destination>(b =>
        {
            b.ToTable("Destinations");
            b.ConfigureByConvention();
            b.Property(x => x.Name).IsRequired().HasMaxLength(200);
            b.Property(x => x.Country).IsRequired().HasMaxLength(100);
            b.Property(x => x.City).IsRequired().HasMaxLength(100);
            b.Property(x => x.Population).IsRequired();
            b.Property(x => x.Photo).HasMaxLength(500);
            b.Property(x => x.UpdateDate).IsRequired();

            b.OwnsOne(d => d.Coordinates, co =>
            {
                co.Property(c => c.Latitude).HasColumnName("Latitude").IsRequired().HasColumnType("float");
                co.Property(c => c.Longitude).HasColumnName("Longitude").IsRequired().HasColumnType("float");
            });
        });

        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            if (typeof(IUserOwned).IsAssignableFrom(entityType.ClrType))
            {
                var parameter = Expression.Parameter(entityType.ClrType, "e");
                var userIdProperty = Expression.Property(parameter, nameof(IUserOwned.UserId));

                // Usamos _currentUser que inyectamos en el constructor
                // Nota: Usamos Expression.Invoke o un closure para asegurar que evalúe el ID en cada consulta
                Expression<Func<Guid>> currentUserIdProvider = () => _currentUser.Id ?? Guid.Empty;
                var currentUserIdValue = Expression.Invoke(Expression.Constant(currentUserIdProvider));

                var body = Expression.Equal(userIdProperty, currentUserIdValue);
                var lambda = Expression.Lambda(body, parameter);

                entityType.SetQueryFilter(lambda);
            }
        }

    }
}
