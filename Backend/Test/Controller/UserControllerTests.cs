//using Business.Repository.Interfaces.Specific.SecurityModule;
//using Entity.DTOs.SecurityModule.User;
//using Microsoft.AspNetCore.Http;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.Extensions.Logging;
//using Moq;
//using System.Security.Claims;
//using Utilities.Enums;
//using Utilities.Exceptions;
//using Web.Controllers.SecurityModel;

//namespace Test.Controller
//{
//    public class UserControllerTests
//    {
//        private readonly Mock<IUserBusiness> _serviceMock;
//        private readonly Mock<ILogger<UserController>> _loggerMock;
//        private readonly UserController _controller;

//        public UserControllerTests()
//        {
//            _serviceMock = new Mock<IUserBusiness>();
//            _loggerMock = new Mock<ILogger<UserController>>();

//            _controller = new UserController(_serviceMock.Object, _loggerMock.Object);

//            // Evita null reference de HttpContext en métodos con Claims
//            _controller.ControllerContext = new ControllerContext
//            {
//                HttpContext = new DefaultHttpContext()
//            };

//            // Usuario por defecto: Id=1, Rol=SM_ACTION
//            _controller.HttpContext.User = new ClaimsPrincipal(
//                new ClaimsIdentity(new[]
//                {
//                new Claim(ClaimTypes.NameIdentifier, "1"),
//                new Claim(ClaimTypes.Role, "SM_ACTION")
//                })
//            );
//        }

//        // --------------------------------------
//        // GET ALL
//        // --------------------------------------
//        [Fact]
//        public async Task GetAll_ShouldReturnOk()
//        {
//            _serviceMock.Setup(s => s.GetAllAsync())
//                .ReturnsAsync(new[] { new UserDTO { Id = 1 } });

//            var result = await _controller.GetAll();

//            var ok = Assert.IsType<OkObjectResult>(result);
//            Assert.Single((IEnumerable<UserDTO>)ok.Value);
//        }

     

//        // --------------------------------------
//        // GET BY USERNAME
//        // --------------------------------------
//        [Fact]
//        public async Task GetByUsername_ShouldReturnOk()
//        {
//            _serviceMock.Setup(s => s.GetByUsernameAsync("admin"))
//                .ReturnsAsync(new UserDTO { Id = 10 });

//            var result = await _controller.GetByUsername("admin");

//            var ok = Assert.IsType<OkObjectResult>(result);
//            Assert.Equal(10, ((UserDTO)ok.Value).Id);
//        }

//        // --------------------------------------
//        // GET BY ID
//        // --------------------------------------
//        [Fact]
//        public async Task GetById_ShouldReturnOk()
//        {
//            _serviceMock.Setup(s => s.GetByIdAsync(50))
//                .ReturnsAsync(new UserDTO { Id = 50 });

//            var result = await _controller.GetById(50);

//            var ok = Assert.IsType<OkObjectResult>(result);
//            Assert.Equal(50, ((UserDTO)ok.Value).Id);
//        }

//        // --------------------------------------
//        // HAS COMPANY
//        // --------------------------------------
//        [Fact]
//        public async Task HasCompany_ShouldReturnOk()
//        {
//            _serviceMock.Setup(s => s.HasCompanyAsync(1))
//                .ReturnsAsync(new UserCompanyCheckDTO { HasCompany = true, CompanyId = 5 });

//            var result = await _controller.HasCompany();

//            var ok = Assert.IsType<OkObjectResult>(result);
//            var dto = Assert.IsType<UserCompanyCheckDTO>(ok.Value);

//            Assert.True(dto.HasCompany);
//            Assert.Equal(5, dto.CompanyId);
//        }

//        [Fact]
//        public async Task HasCompany_ShouldReturnUnauthorized_WhenNoUserId()
//        {
//            _controller.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

//            var result = await _controller.HasCompany();

//            Assert.IsType<UnauthorizedObjectResult>(result);
//        }

       

//        // --------------------------------------
//        // UPDATE
//        // --------------------------------------
//        [Fact]
//        public async Task Update_ShouldReturnOk()
//        {
//            var dto = new UserOptionsDTO { Id = 1, Username = "A", PersonId = 1 };

//            _serviceMock.Setup(s => s.UpdateAsync(dto))
//                .ReturnsAsync(dto);

//            var result = await _controller.Update(dto);

//            var ok = Assert.IsType<OkObjectResult>(result);
//            Assert.Equal(dto, ok.Value);
//        }

//        // --------------------------------------
//        // PARTIAL UPDATE
//        // --------------------------------------
//        [Fact]
//        public async Task PartialUpdate_ShouldReturnOk()
//        {
//            var dto = new UserPartialUpdateDTO { Id = 1, Name = "X" };
//            _serviceMock.Setup(s => s.PartialUpdateAsync(dto))
//                .ReturnsAsync(new UserDTO { Id = 1 });

//            var result = await _controller.PartialUpdate(dto);

//            var ok = Assert.IsType<OkObjectResult>(result);
//            Assert.Equal(1, ((UserDTO)ok.Value).Id);
//        }


//        // --------------------------------------
//        // DELETE
//        // --------------------------------------
//        [Fact]
//        public async Task Delete_ShouldReturnOk()
//        {
//            _serviceMock.Setup(s => s.DeleteAsync(20, DeleteType.Logical))
//                .ReturnsAsync(true);

//            var result = await _controller.Delete(20);

//            var ok = Assert.IsType<OkObjectResult>(result);
//            Assert.True((bool)ok.Value);
//        }

//        [Fact]
//        public async Task Delete_ShouldReturnNotFound_WhenEntityNotFound()
//        {
//            _serviceMock.Setup(s => s.DeleteAsync(999, DeleteType.Logical))
//                .ThrowsAsync(new EntityNotFoundException("User", 999));

//            var result = await _controller.Delete(999);

//            Assert.IsType<NotFoundObjectResult>(result);
//        }
//    }
//}