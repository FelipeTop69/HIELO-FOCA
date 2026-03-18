//using Business.Repository.Interfaces.Specific.ScanItem;
//using Business.Services.Entities.Interfaces.Connection;
//using Entity.DTOs.ScanItem;
//using Entity.DTOs.ScanItem.Missing;
//using Entity.DTOs.System.Connection;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;
//using System.Security.Claims;
//using Utilities.Exceptions;

//namespace Web.Controllers.System.Others
//{
//    [ApiController]
//    [Route("api/[controller]")]
//    public class InventoryController : ControllerBase
//    {
//        private readonly IInventoryScanService _scanService;
//        private readonly IInventoryStartService _startService;
//        private readonly IInventoryFinishService _finishService;
//        private readonly IInventoryVerificationService _verifyService;
//        private readonly IInventoryJoinService _joinService;

//        public InventoryController(
//            IInventoryScanService scanService, 
//            IInventoryStartService startService, 
//            IInventoryFinishService finishService, 
//            IInventoryVerificationService verifyService,
//            IInventoryJoinService joinService)
//        {
//            _scanService = scanService;
//            _startService = startService;
//            _finishService = finishService;
//            _verifyService = verifyService;
//            _joinService = joinService;
//        }

//        [HttpPost("start")]
//        public async Task<IActionResult> Start([FromBody] StartInventoryRequestDto request)
//        {
//            try
//            {
//                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
//                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId) || userId == 0)
//                {
//                    return Unauthorized(new { Message = "No se pudo identificar al usuario desde el token." });
//                }

//                var result = await _startService.StartAsync(request, userId);
//                return Ok(result);
//            }
//            catch (Exception ex)
//            {
//                return BadRequest(new { Message = ex.Message });
//            }
//        }

//        [HttpPost("scan")]
//        public async Task<IActionResult> Scan([FromBody] ScanRequestDto request)
//        {
//            try
//            {
//                // Obtener el userId del token
//                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
//                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId) || userId == 0)
//                {
//                    return Unauthorized(new { Message = "No se pudo identificar al usuario desde el token." });
//                }

//                // Pasar el userId al servicio
//                var result = await _scanService.ScanAsync(request, userId);
//                return Ok(result);
//            }
//            catch (Exception ex)
//            {
//                return BadRequest(new { Message = ex.Message });
//            }
//        }

//        [HttpPost("finish")]
//        public async Task<IActionResult> Finish([FromBody] FinishInventoryRequestDto request)
//        {
//            var result = await _finishService.FinishAsync(request);
//            return Ok(result);
//        }

//        [HttpGet("verification/branch/{branchId}")]
//        public async Task<ActionResult<List<InventarySummaryDto>>> GetInventoriesForVerification(int branchId)
//        {
//            if (branchId <= 0)
//                return BadRequest("El ID de sucursal debe ser mayor que cero.");

//            var inventories = await _verifyService.GetInventoriesForVerificationByBranchAsync(branchId);

//            return Ok(inventories);
//        }

//        [HttpGet("{inventaryId}/compare")]
//        public async Task<ActionResult<VerificationComparisonDto>> CompareAsync(int inventaryId)
//        {
//            var result = await _verifyService.CompareAsync(inventaryId);
//            return Ok(result);
//        }

//        [HttpGet("{inventaryId}/is-scanned/{code}")]
//        public async Task<IActionResult> IsScanned(int inventaryId, string code)
//        {
//            var result = await _scanService.CheckIfScannedAsync(inventaryId, code);

//            if (result.ItemId == null)
//                return NotFound(new { message = result.Message });

//            return Ok(result);
//        }


//        [HttpPost("verify")]
//        public async Task<IActionResult> Verify([FromBody] VerificationRequestDto request)
//        {
//            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
//            var roleClaim = User.FindFirst(ClaimTypes.Role);

//            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId) || userId == 0)
//            {
//                return Unauthorized(new { Message = "No se pudo identificar al usuario desde el token." });
//            }

//            var role = roleClaim?.Value ?? "";

//            var result = await _verifyService.VerifyAsync(request, userId, role);
//            return Ok(result);
//        }

//        /// <summary>
//        /// Valida si un operativo (invitado) puede unirse a un inventario.
//        /// Requerido ANTES de unirse al grupo de SignalR.
//        /// </summary>
//        [Authorize(Roles = "OPERATIVO")] 
//        [HttpPost("join")]
//        [ProducesResponseType(200)]
//        [ProducesResponseType(400)] 
//        [ProducesResponseType(401)] 
//        public async Task<IActionResult> Join([FromBody] JoinInventoryRequestDTO request)
//        {
//            try
//            {
//                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
//                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int guestUserId) || guestUserId == 0)
//                {
//                    return Unauthorized(new { Message = "No se pudo identificar al usuario desde el token." });
//                }

//                var inventary = await _joinService.ValidateJoinAsync(request, guestUserId);
//                return Ok(new
//                {
//                    Message = "Validación exitosa. Bienvenido al inventario.",
//                    ZoneId = inventary.ZoneId,
//                    InventaryId = inventary.Id
//                });
//            }
//            catch (ValidationException ex)
//            {
//                return BadRequest(new { Message = ex.Message });
//            }
//            catch (Exception ex)
//            {
//                return StatusCode(500, new { Message = "Ocurrió un error inesperado.", Details = ex.Message });
//            }
//        }

//        [Authorize(Roles = "OPERATIVO")]
//        [HttpGet("{inventaryId}/missing")]
//        public async Task<IActionResult> GetMissingItems(int inventaryId)
//        {
//            try
//            {
//                var result = await _scanService.GetMissingItemsAsync(inventaryId);
//                return Ok(result);
//            }
//            catch (Exception ex)
//            {
//                return BadRequest(new { Message = ex.Message });
//            }
//        }

//        [Authorize(Roles = "OPERATIVO")]
//        [HttpPost("manual-scan")]
//        public async Task<IActionResult> RegisterManualScans([FromBody] ManualScanRequestDTO request)
//        {
//            try
//            {
//                await _scanService.RegisterManualScansAsync(request);
//                return Ok(new { Message = "Ítems registrados correctamente." });
//            }
//            catch (Exception ex)
//            {
//                return BadRequest(new { Message = ex.Message });
//            }
//        }
//    }
//}