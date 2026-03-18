using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Data.Repository.Implementations;
using Entity.Context;
using Entity.Models.System;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Microsoft.EntityFrameworkCore.Metadata; // <-- Este using es vital

namespace Test.Data
{
    public class GeneralTests
    {
        private AppDbContext CreateContext()
        {
            var connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(connection)
                .EnableSensitiveDataLogging()
                .Options;

            var config = new ConfigurationBuilder().Build();
            var context = new AppDbContext(options, config);

            // FIX: Limpiar colaciones de SQL Server
            var collationAnnotationName = RelationalAnnotationNames.Collation;
            foreach (var entity in context.Model.GetEntityTypes().OfType<IMutableEntityType>())
            {
                entity.RemoveAnnotation(collationAnnotationName);
                foreach (var prop in entity.GetProperties().OfType<IMutableProperty>())
                {
                    prop.RemoveAnnotation(collationAnnotationName);
                }
                foreach (var index in entity.GetIndexes().OfType<IMutableIndex>())
                {
                    index.RemoveAnnotation(collationAnnotationName);
                }
            }

            context.Database.EnsureCreated();
            return context;
        }

        private General<Item> CreateRepository(AppDbContext context)
        {
            // CORRECCIÓN: El logger debe ser ILogger<Item> como estaba originalmente.
            var loggerMock = new Mock<ILogger<Item>>();
            return new General<Item>(context, loggerMock.Object);
        }

        // --------------------------------------------------------
        // GET ALL TOTAL
        // --------------------------------------------------------
        [Fact]
        public async Task GetAllTotalAsync_Should_Return_All_Records()
        {
            var context = CreateContext();
            var repo = CreateRepository(context);

            context.Item.AddRange(
                new Item { Name = "Router", Code = "A1" },
                new Item { Name = "Switch", Code = "B1" }
            );
            await context.SaveChangesAsync();

            var result = await repo.GetAllTotalAsync();

            Assert.Equal(2, result.Count());
        }

        // --------------------------------------------------------
        // GET ALL ITEMS SPECIFIC
        // --------------------------------------------------------
        [Fact]
        public async Task GetAllItemsSpecific_Should_Return_All_Records()
        {
            var context = CreateContext();
            var repo = CreateRepository(context);

            context.Item.AddRange(
                new Item { Name = "Laptop", Code = "C1" },
                new Item { Name = "Mouse", Code = "D1" }
            );
            await context.SaveChangesAsync();

            var result = await repo.GetAllItemsSpecific(10);

            Assert.Equal(2, result.Count());
        }

        // --------------------------------------------------------
        // GET ZONES BY BRANCH OPERATIVE
        // --------------------------------------------------------
        [Fact]
        public async Task GetZonesByBranchOperativeAsync_Should_Return_All_Records()
        {
            var context = CreateContext();
            var repo = CreateRepository(context);

            context.Item.AddRange(
                new Item { Name = "Tablet", Code = "E1" },
                new Item { Name = "Keyboard", Code = "F1" }
            );
            await context.SaveChangesAsync();

            var result = await repo.GetZonesByBranchOperativeAsync(5);

            Assert.Equal(2, result.Count());
        }

        // --------------------------------------------------------
        // EXCEPTION HANDLING
        // --------------------------------------------------------
        [Fact]
        public async Task GetAllTotalAsync_Should_Log_Error_On_Exception()
        {
            // Mock del context que lanza excepción
            // CORRECCIÓN: El logger debe ser ILogger<Item>
            var loggerMock = new Mock<ILogger<Item>>();
            var contextMock = new Mock<AppDbContext>(new DbContextOptions<AppDbContext>(), new ConfigurationBuilder().Build());

            contextMock.Setup(c => c.Set<Item>()).Throws(new Exception("DB error"));

            var repo = new General<Item>(contextMock.Object, loggerMock.Object);

            await Assert.ThrowsAsync<Exception>(() => repo.GetAllTotalAsync());

            // Verificar que el logger se llamó
            loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()
                ),
                Times.Once
            );
        }
    }
}