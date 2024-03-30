using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using EFCore.Snowflake.Extensions;
using EFCore.Snowflake.FunctionalTests.TestUtilities;
using Microsoft.EntityFrameworkCore.TestUtilities;

namespace EFCore.Snowflake.FunctionalTests;

public class ComputedColumnTest : IDisposable
{
    public ComputedColumnTest()
    {
        TestStore = SnowflakeTestStore.CreateInitialized("ComputedColumnTest");
    }

    protected SnowflakeTestStore TestStore { get; }

    [ConditionalFact]
    public void Can_use_computed_columns()
    {
        var serviceProvider = new ServiceCollection()
            .AddEntityFrameworkSnowflake()
            .BuildServiceProvider(validateScopes: true);

        using var context = new Context(serviceProvider, TestStore.Name);
        context.Database.EnsureCreatedResiliently();

        var entity = context.Add(
            new Entity
            {
                P1 = 20,
                P2 = 30,
                P3 = 80
            }).Entity;

        context.SaveChanges();

        Assert.Equal(50, entity.P4);
        Assert.Equal(100, entity.P5);
    }

    [ConditionalFact]
    public void Can_use_computed_columns_with_null_values()
    {
        var serviceProvider = new ServiceCollection()
            .AddEntityFrameworkSnowflake()
            .BuildServiceProvider(validateScopes: true);

        using var context = new Context(serviceProvider, TestStore.Name);
        context.Database.EnsureCreatedResiliently();

        var entity = context.Add(new Entity { P1 = 20, P2 = 30 }).Entity;

        context.SaveChanges();

        Assert.Equal(50, entity.P4);
        Assert.Null(entity.P5);
    }

    private class Context : DbContext
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly string _databaseName;

        public Context(IServiceProvider serviceProvider, string databaseName)
        {
            _serviceProvider = serviceProvider;
            _databaseName = databaseName;
        }

        public DbSet<Entity> Entities { get; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder
                .UseSnowflake(SnowflakeTestStore.CreateConnectionString(_databaseName), b => b.ApplyConfiguration())
                .UseInternalServiceProvider(_serviceProvider);

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Entity>()
                .Property(e => e.P4)
                .HasComputedColumnSql(@"""P1"" + ""P2""");

            modelBuilder.Entity<Entity>()
                .Property(e => e.P5)
                .HasComputedColumnSql(@"""P1"" + ""P3""");
        }
    }

    private class Entity
    {
        public int Id { get; set; }
        public int P1 { get; set; }
        public int P2 { get; set; }
        public int? P3 { get; set; }
        public int P4 { get; set; }
        public int? P5 { get; set; }
    }
    
    public virtual void Dispose()
        => TestStore.Dispose();
}
