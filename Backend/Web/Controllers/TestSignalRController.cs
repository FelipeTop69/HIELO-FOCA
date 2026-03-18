//using Microsoft.AspNetCore.Mvc;
//using Microsoft.AspNetCore.SignalR;
//using Web.Hubs; // Asegúrate de importar tu Hub

//namespace Web.Controllers
//{
//    [Route("api/[controller]")]
//    [ApiController]
//    public class TestSignalRController : ControllerBase
//    {
//        private readonly IHubContext<AppHub> _hubContext;

//        public TestSignalRController(IHubContext<AppHub> hubContext)
//        {
//            _hubContext = hubContext;
//        }

//        /// <summary>
//        /// Endpoint de prueba para disparar una alerta a todos los clientes conectados.
//        /// </summary>
//        /// <param name="mensaje">El texto que aparecerá en la alerta del móvil</param>
//        [HttpPost("enviar-alerta")]
//        public async Task<IActionResult> SendAlert([FromBody] string mensaje)
//        {
//            // "ReceiveTestAlert" es el nombre del evento que escucharemos en Ionic
//            await _hubContext.Clients.All.SendAsync("ReceiveTestAlert", mensaje);
//            return Ok(new { message = "Alerta enviada a todos los clientes conectados via SignalR" });
//        }
//    }
//}