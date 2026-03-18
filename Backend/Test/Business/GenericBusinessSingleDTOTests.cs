using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Business.Repository.Implementations;
using Data.Repository.Interfaces;
using Data.Repository.Interfaces.Strategy.Delete;
using Microsoft.Extensions.Logging;
using Moq;
using Utilities.Enums;
using Utilities.Exceptions;
using Xunit;

namespace Test.Business
{
    // =======================================================
    //            Entity + DTO exclusivos de este test
    // =======================================================
    public class SingleEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }

    public class SingleDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }

    // =======================================================
    //    Implementación concreta de GenericBusinessSingleDTO
    // =======================================================
    public class SingleBusinessService
        : GenericBusinessSingleDTO<SingleEntity, SingleDto>
    {
        public bool BeforeCreateCalled = false;
        public bool BeforeUpdateCalled = false;

        public SingleBusinessService(
            IGenericData<SingleEntity> data,
            IDeleteStrategyResolver<SingleEntity> resolver,
            ILogger<SingleEntity> logger,
            IMapper mapper)
            : base(data, resolver, logger, mapper) { }

        protected override Task BeforeCreateMap(SingleDto dto, SingleEntity entity)
        {
            BeforeCreateCalled = true;
            return Task.CompletedTask;
        }

        protected override Task BeforeUpdateMap(SingleDto dto, SingleEntity entity)
        {
            BeforeUpdateCalled = true;
            return Task.CompletedTask;
        }
    }

    // =======================================================
    //                  TEST SUITE
    // =======================================================
    public class GenericBusinessSingleDTOTests
    {
        private IMapper CreateMapper()
        {
            var cfg = new MapperConfiguration(c =>
            {
                c.CreateMap<SingleEntity, SingleDto>();
                c.CreateMap<SingleDto, SingleEntity>();
            });

            return cfg.CreateMapper();
        }

        private SingleBusinessService CreateService(
            out Mock<IGenericData<SingleEntity>> dataMock,
            out Mock<IDeleteStrategyResolver<SingleEntity>> resolverMock,
            out Mock<IDeleteStrategy<SingleEntity>> strategyMock,
            out Mock<ILogger<SingleEntity>> loggerMock)
        {
            dataMock = new Mock<IGenericData<SingleEntity>>();
            resolverMock = new Mock<IDeleteStrategyResolver<SingleEntity>>();
            strategyMock = new Mock<IDeleteStrategy<SingleEntity>>();
            loggerMock = new Mock<ILogger<SingleEntity>>();

            resolverMock
                .Setup(r => r.Resolve(It.IsAny<DeleteType>()))
                .Returns(strategyMock.Object);

            return new SingleBusinessService(
                dataMock.Object,
                resolverMock.Object,
                loggerMock.Object,
                CreateMapper()
            );
        }

        // =======================================================
        //                     GET ALL
        // =======================================================
        [Fact]
        public async Task GetAllAsync_Should_Map_List_To_Dto()
        {
            var service = CreateService(out var data, out _, out _, out _);

            data.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<SingleEntity>
            {
                new SingleEntity { Id = 1, Name = "Test" }
            });

            var result = await service.GetAllAsync();

            Assert.Single(result);
            Assert.Equal("Test", result.First().Name);
        }

        // =======================================================
        //                     GET BY ID
        // =======================================================
        [Fact]
        public async Task GetByIdAsync_Should_Return_Dto_When_Found()
        {
            var service = CreateService(out var data, out _, out _, out _);

            data.Setup(x => x.GetByIdAsync(5))
                .ReturnsAsync(new SingleEntity { Id = 5, Name = "Entity" });

            var dto = await service.GetByIdAsync(5);

            Assert.Equal("Entity", dto.Name);
        }

        [Fact]
        public async Task GetByIdAsync_Should_Throw_When_NotFound()
        {
            var service = CreateService(out var data, out _, out _, out _);

            data.Setup(x => x.GetByIdAsync(10))
                .ReturnsAsync((SingleEntity)null!);

            await Assert.ThrowsAsync<EntityNotFoundException>(() => service.GetByIdAsync(10));
        }

        // =======================================================
        //                     CREATE
        // =======================================================
        [Fact]
        public async Task CreateAsync_Should_Map_And_Call_Hook()
        {
            var service = CreateService(out var data, out _, out _, out _);

            var dto = new SingleDto { Name = "New" };
            var entity = new SingleEntity { Id = 1, Name = "New" };

            data.Setup(x => x.CreateAsync(It.IsAny<SingleEntity>()))
                .ReturnsAsync(entity);

            var result = await service.CreateAsync(dto);

            Assert.True(service.BeforeCreateCalled);
            Assert.Equal("New", result.Name);
        }

        [Fact]
        public async Task CreateAsync_Should_Throw_When_Dto_Is_Null()
        {
            var service = CreateService(out _, out _, out _, out _);

            await Assert.ThrowsAsync<ValidationException>(() => service.CreateAsync(null!));
        }

        // =======================================================
        //                     UPDATE
        // =======================================================
        [Fact]
        public async Task UpdateAsync_Should_Update_Entity_And_Call_Hook()
        {
            var service = CreateService(out var data, out _, out _, out _);

            var dto = new SingleDto { Id = 2, Name = "Updated" };
            var existing = new SingleEntity { Id = 2, Name = "Old" };

            data.Setup(x => x.GetByIdAsync(2))
                .ReturnsAsync(existing);

            data.Setup(x => x.UpdateAsync(existing))
                .ReturnsAsync(existing);

            var result = await service.UpdateAsync(dto);

            Assert.True(service.BeforeUpdateCalled);
            Assert.Equal("Updated", result.Name);
        }

        [Fact]
        public async Task UpdateAsync_Should_Throw_When_Id_Invalid()
        {
            var service = CreateService(out _, out _, out _, out _);

            var dto = new SingleDto { Id = 0, Name = "X" };

            await Assert.ThrowsAsync<ValidationException>(() => service.UpdateAsync(dto));
        }

        [Fact]
        public async Task UpdateAsync_Should_Throw_When_NotFound()
        {
            var service = CreateService(out var data, out _, out _, out _);

            data.Setup(x => x.GetByIdAsync(10))
                .ReturnsAsync((SingleEntity)null!);

            var dto = new SingleDto { Id = 10, Name = "X" };

            await Assert.ThrowsAsync<EntityNotFoundException>(() => service.UpdateAsync(dto));
        }

        // =======================================================
        //                     DELETE
        // =======================================================
        [Fact]
        public async Task DeleteAsync_Should_Call_StrategyResolver_And_Return_True()
        {
            var service = CreateService(out var data, out var resolver, out var strategy, out _);

            data.Setup(x => x.GetByIdAsync(3))
                .ReturnsAsync(new SingleEntity { Id = 3 });

            strategy.Setup(x => x.DeleteAsync(3, data.Object))
                .ReturnsAsync(true);

            var ok = await service.DeleteAsync(3, DeleteType.Logical);

            Assert.True(ok);
            resolver.Verify(r => r.Resolve(DeleteType.Logical), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_Should_Throw_When_NotFound()
        {
            var service = CreateService(out var data, out _, out _, out _);

            data.Setup(x => x.GetByIdAsync(99))
                .ReturnsAsync((SingleEntity)null!);

            await Assert.ThrowsAsync<EntityNotFoundException>(() => service.DeleteAsync(99, DeleteType.Logical));
        }
    }
}
