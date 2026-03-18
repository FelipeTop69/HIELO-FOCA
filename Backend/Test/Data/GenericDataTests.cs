using System;
using System.Linq;
using System.Threading.Tasks;
using Data.Repository.Implementations;
using Entity.Context;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Microsoft.EntityFrameworkCore.Metadata; // <-- 1. AÑADIR ESTE USING

namespace Test.Data
{
    // ================================
    //    FAKE ENTITY PARA LAS PRUEBAS
    // ================================
    public class FakeEntity
    {
        public int Id { get; set; }
        public bool Active { get; set; } = true;
        public string Name { get; set; } = string.Empty;
    }

    public class GenericDataTests
    {
        // ==============================================
        // PATCH: PARCHE PARA QUITAR COLLATIONS EN SQLITE
        // ==============================================

        // --- INICIO DE LA CORRECCIÓN ---
        // Reemplaza tu método con este:
        private void DisableCollationsForSqlite(AppDbContext ctx)
        {
            if (ctx.Database.IsSqlite())
            {
                var collationAnnotationName = RelationalAnnotationNames.Collation;

                // Debemos iterar sobre el modelo MUTABLE para hacer cambios
                foreach (var entity in ctx.Model.GetEntityTypes().OfType<IMutableEntityType>())
                {
                    // Limpiar colación de entidad
                    entity.RemoveAnnotation(collationAnnotationName);

                    // Limpiar colación de propiedades
                    foreach (var prop in entity.GetProperties().OfType<IMutableProperty>())
                    {
                        prop.RemoveAnnotation(collationAnnotationName);
                    }

                    // ¡CRUCIAL! Limpiar colación de índices
                    foreach (var index in entity.GetIndexes().OfType<IMutableIndex>())
                    {
                        index.RemoveAnnotation(collationAnnotationName);
                    }
                }
            }
        }
        // --- FIN DE LA CORRECCIÓN ---


        // ====================================
        //    CREA DB CONTEXT (SQLite InMemory)
        // ====================================
        private AppDbContext CreateContext()
        {
            var conn = new SqliteConnection("DataSource=:memory:");
            conn.Open();

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(conn)
                .EnableSensitiveDataLogging()
                .Options;

            var config = new ConfigurationBuilder().Build();
            var context = new AppDbContext(options, config);

            DisableCollationsForSqlite(context); // <--- PATCH aplicado (ahora corregido)

            context.Database.EnsureCreated();    // NO USAR MIGRATIONS EN SQLITE TEST

            return context;
        }

        private GenericData<FakeEntity> CreateRepo(AppDbContext ctx, out Mock<ILogger<FakeEntity>> logger)
        {
            logger = new Mock<ILogger<FakeEntity>>();
            return new GenericData<FakeEntity>(ctx, logger.Object);
        }

        // ====================================
        //           GetAllAsync()
        // ====================================
        [Fact]
        public async Task GetAllAsync_Should_Return_Only_Active_Records()
        {
            var ctx = CreateContext();
            ctx.Set<FakeEntity>().AddRange(
                new FakeEntity { Id = 1, Name = "A", Active = true },
                new FakeEntity { Id = 2, Name = "B", Active = false },
                new FakeEntity { Id = 3, Name = "C", Active = true }
            );
            await ctx.SaveChangesAsync();

            var repo = CreateRepo(ctx, out _);

            var result = await repo.GetAllAsync();

            Assert.Equal(2, result.Count());
            Assert.DoesNotContain(result, x => x.Id == 2);
        }

        // ====================================
        //         GetByIdAsync()
        // ====================================
        [Fact]
        public async Task GetByIdAsync_Should_Return_Entity_When_Exists()
        {
            var ctx = CreateContext();
            ctx.Set<FakeEntity>().Add(new FakeEntity { Id = 10, Name = "Test" });
            await ctx.SaveChangesAsync();

            var repo = CreateRepo(ctx, out _);

            var entity = await repo.GetByIdAsync(10);

            Assert.NotNull(entity);
            Assert.Equal("Test", entity!.Name);
        }

        [Fact]
        public async Task GetByIdAsync_Should_Return_Null_When_Not_Found()
        {
            var ctx = CreateContext();
            var repo = CreateRepo(ctx, out _);

            var entity = await repo.GetByIdAsync(999);

            Assert.Null(entity);
        }

        // ====================================
        //          CreateAsync()
        // ====================================
        [Fact]
        public async Task CreateAsync_Should_Insert_Entity()
        {
            var ctx = CreateContext();
            var repo = CreateRepo(ctx, out _);

            var entity = new FakeEntity { Id = 77, Name = "Nuevo" };

            var result = await repo.CreateAsync(entity);

            Assert.Equal("Nuevo", result.Name);
            Assert.Equal(1, ctx.Set<FakeEntity>().Count());
        }

        // ====================================
        //          UpdateAsync()
        // ====================================
        [Fact]
        public async Task UpdateAsync_Should_Update_Entity()
        {
            var ctx = CreateContext();
            ctx.Set<FakeEntity>().Add(new FakeEntity { Id = 5, Name = "Original" });
            await ctx.SaveChangesAsync();

            var repo = CreateRepo(ctx, out _);

            var entity = await ctx.Set<FakeEntity>().FindAsync(5);
            entity!.Name = "Modificado";

            var result = await repo.UpdateAsync(entity);

            Assert.Equal("Modificado", result.Name);
        }

        // ====================================
        //     DeletePersistenceAsync()
        // ====================================
        [Fact]
        public async Task DeletePersistenceAsync_Should_Remove_Entity()
        {
            var ctx = CreateContext();
            ctx.Set<FakeEntity>().Add(new FakeEntity { Id = 4 });
            await ctx.SaveChangesAsync();

            var repo = CreateRepo(ctx, out _);

            var deleted = await repo.DeletePersistenceAsync(4);

            Assert.True(deleted);
            Assert.Null(await ctx.Set<FakeEntity>().FindAsync(4));
        }

        [Fact]
        public async Task DeletePersistenceAsync_Should_Return_False_If_Not_Found()
        {
            var ctx = CreateContext();
            var repo = CreateRepo(ctx, out _);

            var deleted = await repo.DeletePersistenceAsync(111);

            Assert.False(deleted);
        }

        // ====================================
        //      DeleteLogicalAsync()
        // ====================================
        [Fact]
        public async Task DeleteLogicalAsync_Should_Set_Active_False()
        {
            var ctx = CreateContext();
            ctx.Set<FakeEntity>().Add(new FakeEntity { Id = 8, Active = true });
            await ctx.SaveChangesAsync();

            var repo = CreateRepo(ctx, out _);

            var deleted = await repo.DeleteLogicalAsync(8);

            Assert.True(deleted);

            var entity = await ctx.Set<FakeEntity>().FindAsync(8);
            Assert.False(entity!.Active);
        }

        [Fact]
        public async Task DeleteLogicalAsync_Should_Return_False_If_Not_Found()
        {
            var ctx = CreateContext();
            var repo = CreateRepo(ctx, out _);

            var deleted = await repo.DeleteLogicalAsync(999);

            Assert.False(deleted);
        }

        // ====================================
        //      DeleteCascadeAsync()
        // ====================================
        [Fact]
        public async Task DeleteCascadeAsync_Should_Remove_Entity()
        {
            var ctx = CreateContext();
            ctx.Set<FakeEntity>().Add(new FakeEntity { Id = 12 });
            await ctx.SaveChangesAsync();

            var repo = CreateRepo(ctx, out _);

            var deleted = await repo.DeleteCascadeAsync(12);

            Assert.True(deleted);
            Assert.Null(await ctx.Set<FakeEntity>().FindAsync(12));
        }

        // ====================================
        //         GetQueryable()
        // ====================================
        [Fact]
        public void GetQueryable_Should_Return_IQueryable()
        {
            var ctx = CreateContext();
            var repo = CreateRepo(ctx, out _);

            var query = repo.GetQueryable();

            Assert.IsAssignableFrom<IQueryable<FakeEntity>>(query);
        }
    }
}