using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities.Exceptions;
using Web.Controllers;

namespace Test.Controller
{
    public class BaseControllerTests
    {
        private readonly Mock<IServiceTest> _serviceMock;
        private readonly Mock<ILogger> _loggerMock;
        private readonly FakeTestController _controller;

        public BaseControllerTests()
        {
            _serviceMock = new Mock<IServiceTest>();
            _loggerMock = new Mock<ILogger>();

            _controller = new FakeTestController(
                _serviceMock.Object,
                _loggerMock.Object);
        }

        // --------------------------------------
        // SUCCESS → RETURN OK
        // --------------------------------------
        [Fact]
        public async Task TryExecuteAsync_ShouldReturnOk_WhenSuccess()
        {
            var result = await _controller.ExecuteAsync(
                () => Task.FromResult<object>("TEST"),
                "Context");

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal("TEST", ok.Value);
        }

        // --------------------------------------
        // VALIDATION ERROR → 400
        // --------------------------------------
        [Fact]
        public async Task TryExecuteAsync_ShouldReturnBadRequest_OnValidationException()
        {
            var result = await _controller.ExecuteAsync(
                () => throw new ValidationException("Campo", "Error de validación"),
                "Context");

            var bad = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("Error de validación", bad.Value!.ToString());
        }

        // --------------------------------------
        // ENTITY NOT FOUND → 404
        // --------------------------------------
        [Fact]
        public async Task TryExecuteAsync_ShouldReturnNotFound_OnEntityNotFoundException()
        {
            var result = await _controller.ExecuteAsync(
                () => throw new EntityNotFoundException("User", 5),
                "Context");

            var nf = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Contains("User", nf.Value!.ToString());
        }

        // --------------------------------------
        // FORBIDDEN → 403
        // --------------------------------------
        [Fact]
        public async Task TryExecuteAsync_ShouldReturnForbid_OnForbiddenException()
        {
            var result = await _controller.ExecuteAsync(
                () => throw new ForbiddenException("Sin permisos"),
                "Context");

            Assert.IsType<ForbidResult>(result);
        }

        // --------------------------------------
        // GENERAL ERROR → 500
        // --------------------------------------
        [Fact]
        public async Task TryExecuteAsync_ShouldReturn500_OnException()
        {
            var result = await _controller.ExecuteAsync(
                () => throw new Exception("fallo"),
                "Context");

            var internalErr = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, internalErr.StatusCode);
        }

        // --------------------------------------
        // IActionResult VERSION
        // --------------------------------------
        [Fact]
        public async Task TryExecuteAsync_IAction_ShouldReturnCustomResult()
        {
            var result = await _controller.ExecuteActionAsync(
                () => Task.FromResult<IActionResult>(new OkObjectResult("X")),
                "Context");

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal("X", ok.Value);
        }

        [Fact]
        public async Task TryExecuteAsync_IAction_ShouldReturn500_OnException()
        {
            var result = await _controller.ExecuteActionAsync(
                () => throw new Exception("error x"),
                "Context");

            var err = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, err.StatusCode);
        }
    }
}
