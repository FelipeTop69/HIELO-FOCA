using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Web.Controllers.Base;

namespace Web.Controllers
{
    public class FakeTestController : BaseController<IServiceTest>
    {
        public FakeTestController(IServiceTest service, ILogger logger)
            : base(service, logger)
        { }

        // Método público para llamar TryExecuteAsync<T>
        public Task<IActionResult> ExecuteAsync(Func<Task<object>> func, string context)
            => TryExecuteAsync(func, context);

        // Versión IActionResult
        public Task<IActionResult> ExecuteActionAsync(Func<Task<IActionResult>> func, string context)
            => TryExecuteAsync(func, context);
    }

    public interface IServiceTest { }
}
