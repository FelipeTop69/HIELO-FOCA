using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using Business.Repository.Implementations;
using Data.Repository.Interfaces;
using Data.Repository.Interfaces.Strategy.Delete;
using Microsoft.Extensions.Logging;
using Moq;
using Utilities.Enums;
using Utilities.Exceptions;
using Utilities.Helpers;
using Xunit;

namespace Test.Business
{
    // ============================================================
    //        FAKE ENTITY + DTOs PARA TESTS
    // ============================================================
    public class FakeEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }

    public class FakeReadDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }

    public class FakeWriteDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }

    // ============================================================
    //        IMPLEMENTACIÓN FAKE DEL BUSINESS PARA PROBAR
    // ============================================================
    public class FakeBusinessService
        : GenericBusinessDualDTO<FakeEntity, FakeReadDto, FakeWriteDto>
    {
        public bool BeforeCreateCalled = false;
        public bool BeforeUpdateCalled = false;

        public FakeBusinessService(
            IGenericData<FakeEntity> data,
            IDeleteStrategyResolver<FakeEntity> deleteResolver,
            ILogger<FakeEntity> logger,
            IMapper mapper)
            : base(data, deleteResolver, logger, mapper)
        {
        }

        protected override Task BeforeCreateMap(FakeWriteDto dto, FakeEntity entity)
        {
            BeforeCreateCalled = true;
            return Task.CompletedTask;
        }

        protected override Task BeforeUpdateMap(FakeWriteDto dto, FakeEntity entity)
        {
            BeforeUpdateCalled = true;
            return Task.CompletedTask;
        }
    }

    // ============================================================
    //        TESTS
    // ============================================================
    public class GenericBusinessDualDTOTests
    {
        private IMapper CreateMapper()
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<FakeEntity, FakeReadDto>();
                cfg.CreateMap<FakeWriteDto, FakeEntity>();
                cfg.CreateMap<FakeEntity, FakeWriteDto>();
            });

            return config.CreateMapper();
        }

        private FakeBusinessService CreateService(
            out Mock<IGenericData<FakeEntity>> dataMock,
            out Mock<IDeleteStrategyResolver<FakeEntity>> deleteResolverMock,
            out Mock<ILogger<FakeEntity>> loggerMock,
            out Mock<IDeleteStrategy<FakeEntity>> deleteStrategyMock)
        {
            dataMock = new Mock<IGenericData<FakeEntity>>();
            deleteResolverMock = new Mock<IDeleteStrategyResolver<FakeEntity>>();
            deleteStrategyMock = new Mock<IDeleteStrategy<FakeEntity>>();
            loggerMock = new Mock<ILogger<FakeEntity>>();

            deleteResolverMock
                .Setup(x => x.Resolve(It.IsAny<DeleteType>()))
                .Returns(deleteStrategyMock.Object);

            var mapper = CreateMapper();

            return new FakeBusinessService(
                dataMock.Object,
                deleteResolverMock.Object,
                loggerMock.Object,
                mapper
            );
        }

        // ============================================================
        //          GET ALL
        // ============================================================
        [Fact]
        public async Task GetAllAsync_Should_Map_Entities_To_ReadDto()
        {
            var service = CreateService(out var data, out _, out _, out _);

            data.Setup(d => d.GetAllAsync())
                .ReturnsAsync(new List<FakeEntity>
                {
                    new FakeEntity { Id = 1, Name = "A" }
                });

            var list = await service.GetAllAsync();

            Assert.Single(list);
            Assert.Equal("A", list.First().Name);
        }

        // ============================================================
        //          GET BY ID - OK
        // ============================================================
        [Fact]
        public async Task GetByIdAsync_Should_Return_Mapped_Dto_When_Exists()
        {
            var service = CreateService(out var data, out _, out _, out _);

            data.Setup(d => d.GetByIdAsync(1))
                .ReturnsAsync(new FakeEntity { Id = 1, Name = "Test" });

            var dto = await service.GetByIdAsync(1);

            Assert.Equal("Test", dto.Name);
        }

        // ============================================================
        //          GET BY ID - NOT FOUND
        // ============================================================
        [Fact]
        public async Task GetByIdAsync_Should_Throw_When_NotFound()
        {
            var service = CreateService(out var data, out _, out _, out _);

            data.Setup(d => d.GetByIdAsync(1))
                .ReturnsAsync((FakeEntity)null!);

            await Assert.ThrowsAsync<EntityNotFoundException>(() => service.GetByIdAsync(1));
        }

        // ============================================================
        //          CREATE
        // ============================================================
        [Fact]
        public async Task CreateAsync_Should_Map_And_Call_Hooks()
        {
            var service = CreateService(out var data, out _, out _, out _);

            var dto = new FakeWriteDto { Id = 0, Name = "New" };
            var entity = new FakeEntity { Id = 1, Name = "New" };

            data.Setup(d => d.CreateAsync(It.IsAny<FakeEntity>()))
                .ReturnsAsync(entity);

            var result = await service.CreateAsync(dto);

            Assert.True(service.BeforeCreateCalled);
            Assert.Equal("New", result.Name);
        }

        [Fact]
        public async Task CreateAsync_Should_Throw_When_Null_Dto()
        {
            var service = CreateService(out _, out _, out _, out _);

            await Assert.ThrowsAsync<ValidationException>(() =>
                service.CreateAsync(null!));
        }

        // ============================================================
        //          UPDATE
        // ============================================================
        [Fact]
        public async Task UpdateAsync_Should_Map_Update_Entity_And_Call_Hooks()
        {
            var service = CreateService(out var data, out _, out _, out _);

            var dto = new FakeWriteDto { Id = 5, Name = "Updated" };
            var entity = new FakeEntity { Id = 5, Name = "Old" };

            data.Setup(d => d.GetByIdAsync(5)).ReturnsAsync(entity);
            data.Setup(d => d.UpdateAsync(entity)).ReturnsAsync(entity);

            var result = await service.UpdateAsync(dto);

            Assert.True(service.BeforeUpdateCalled);
            Assert.Equal("Updated", result.Name);
        }

        [Fact]
        public async Task UpdateAsync_Should_Throw_If_Id_Is_Invalid()
        {
            var service = CreateService(out _, out _, out _, out _);

            var dto = new FakeWriteDto { Id = 0, Name = "X" };

            await Assert.ThrowsAsync<ValidationException>(() => service.UpdateAsync(dto));
        }

        [Fact]
        public async Task UpdateAsync_Should_Throw_If_NotFound()
        {
            var service = CreateService(out var data, out _, out _, out _);

            data.Setup(d => d.GetByIdAsync(1)).ReturnsAsync((FakeEntity)null!);

            var dto = new FakeWriteDto { Id = 1, Name = "X" };

            await Assert.ThrowsAsync<EntityNotFoundException>(() => service.UpdateAsync(dto));
        }

        // ============================================================
        //          DELETE
        // ============================================================
        [Fact]
        public async Task DeleteAsync_Should_Use_StrategyResolver_And_Return_Result()
        {
            var service = CreateService(out var data, out var resolver, out _, out var strategy);

            data.Setup(d => d.GetByIdAsync(10))
                .ReturnsAsync(new FakeEntity { Id = 10 });

            strategy.Setup(s => s.DeleteAsync(10, data.Object))
                .ReturnsAsync(true);

            var ok = await service.DeleteAsync(10, DeleteType.Logical);

            Assert.True(ok);
            resolver.Verify(r => r.Resolve(DeleteType.Logical), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_Should_Throw_If_NotFound()
        {
            var service = CreateService(out var data, out _, out _, out _);

            data.Setup(d => d.GetByIdAsync(10))
                .ReturnsAsync((FakeEntity)null!);

            await Assert.ThrowsAsync<EntityNotFoundException>(() =>
                service.DeleteAsync(10, DeleteType.Logical));
        }
    }
}
