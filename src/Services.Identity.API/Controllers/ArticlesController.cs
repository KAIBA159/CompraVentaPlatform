using Microsoft.AspNetCore.Mvc;

namespace Services.Identity.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ArticlesController : ControllerBase
    {
        private readonly ILogger<ArticlesController> _logger;

        public ArticlesController(ILogger<ArticlesController> logger)
        {
            _logger = logger;
        }

        [HttpPost("masivos")]
        public async Task<IActionResult> CrearArticulosMasivos([FromBody] List<ArticuloDto> articulos)
        {
            if (articulos == null || !articulos.Any())
            {
                return BadRequest(new { success = false, message = "No se recibieron artículos para procesar." });
            }

            var resultados = new List<object>();

            foreach (var item in articulos)
            {
                try
                {
                    // Lógica temporal de simulación para procesamiento masivo hacia SAP B1 10.0
                    resultados.Add(new
                    {
                        itemCode = item.ItemCode,
                        status = "OK",
                        message = "Artículo preparado / simulado correctamente"
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al procesar el artículo {ItemCode}", item.ItemCode);
                    resultados.Add(new
                    {
                        itemCode = item.ItemCode,
                        status = "ERROR",
                        message = ex.Message
                    });
                }
            }

            return Ok(new
            {
                success = true,
                totalProcesados = articulos.Count,
                detalles = resultados
            });
        }
    }

    public class ArticuloDto
    {
        public string ItemCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public int ItemsGroupCode { get; set; }
        public decimal Price { get; set; }
    }
}