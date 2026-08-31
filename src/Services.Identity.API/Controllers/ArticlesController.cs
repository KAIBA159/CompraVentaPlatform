using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Services.Identity.API.Services;
using Services.Identity.API.DTOs;
//using Services.Identity.API.DTOs;


namespace Services.Identity.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ArticlesController : ControllerBase
    {
        private readonly SapServiceLayerAuth _sapAuth;
        private readonly SapArticleService _articleService;
        private readonly SapSettings _sapSettings;

        public ArticlesController(
            SapServiceLayerAuth sapAuth,
            SapArticleService articleService,
            IOptions<SapSettings> sapSettings)
        {
            _sapAuth = sapAuth;
            _articleService = articleService;
            _sapSettings = sapSettings.Value;
        }

        private HttpClient CrearSapHttpClient()
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };

            return new HttpClient(handler) { BaseAddress = new Uri(_sapSettings.Server) };
        }

        [HttpPost("crear-masivo-simples")]
        public async Task<IActionResult> CrearArticulosSimplesMasivos([FromBody] List<ArticuloSimpleMigracionDto> articulos)
        {
            if (articulos == null || !articulos.Any())
                return BadRequest(new { success = false, message = "La lista de artículos está vacía." });

            var resultados = new List<object>();

            try
            {
                string sessionCookie = await _sapAuth.ObtenerCookieSesionAsync();
                using var client = CrearSapHttpClient();

                foreach (var item in articulos)
                {
                    try
                    {
                        var resultado = await _articleService.CrearArticuloSimpleAsync(item, sessionCookie, client);
                        resultados.Add(new { itemCode = item.ItemCode, status = resultado.Exito ? "OK" : "ERROR_ITEM", message = resultado.Exito ? "Registrado correctamente en SAP B1" : resultado.Mensaje });
                    }
                    catch (Exception exItem)
                    {
                        resultados.Add(new { itemCode = item.ItemCode, status = "EXCEPTION", message = exItem.Message });
                    }
                }

                return Ok(new { success = true, totalProcesados = articulos.Count, detalles = resultados });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Error general en la pasarela Service Layer: {ex.Message}" });
            }
        }

        [HttpPost("actualizar-masivo-simples")]
        public async Task<IActionResult> ActualizarArticulosSimplesMasivos([FromBody] List<ArticuloSimpleMigracionDto> articulos)
        {
            if (articulos == null || !articulos.Any())
                return BadRequest(new { success = false, message = "La lista de artículos está vacía." });

            var resultados = new List<object>();

            try
            {
                string sessionCookie = await _sapAuth.ObtenerCookieSesionAsync();
                using var client = CrearSapHttpClient();

                foreach (var item in articulos)
                {
                    try
                    {
                        var resultado = await _articleService.ActualizarArticuloSimpleAsync(item, sessionCookie, client);
                        resultados.Add(new { itemCode = item.ItemCode, status = resultado.Exito ? "OK" : "ERROR_ITEM", message = resultado.Exito ? "Actualizado correctamente en SAP B1" : resultado.Mensaje });
                    }
                    catch (Exception exItem)
                    {
                        resultados.Add(new { itemCode = item.ItemCode, status = "EXCEPTION", message = exItem.Message });
                    }
                }

                return Ok(new { success = true, totalProcesados = articulos.Count, detalles = resultados });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Error general en la pasarela Service Layer: {ex.Message}" });
            }
        }

        [HttpPost("crear-masivo-combos")]
        public async Task<IActionResult> CrearCombosMasivos([FromBody] List<ArticuloComboMigracionDto> combos)
        {
            if (combos == null || !combos.Any())
                return BadRequest(new { success = false, message = "No se recibieron combos para procesar." });

            var resultados = new List<object>();

            try
            {
                string sessionCookie = await _sapAuth.ObtenerCookieSesionAsync();
                using var client = CrearSapHttpClient();

                foreach (var combo in combos)
                {
                    try
                    {
                        var resultado = await _articleService.CrearArticuloComboAsync(combo, sessionCookie, client);
                        resultados.Add(new { itemCode = combo.ItemCode, status = resultado.Exito ? "OK" : "ERROR_ITEM", message = resultado.Mensaje });
                    }
                    catch (Exception exItem)
                    {
                        resultados.Add(new { itemCode = combo.ItemCode, status = "EXCEPTION", message = exItem.Message });
                    }
                }

                return Ok(new { success = true, totalProcesados = combos.Count, detalles = resultados });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Error general en la pasarela Service Layer: {ex.Message}" });
            }
        }

        [HttpPost("actualizar-masivo-combos")]
        public async Task<IActionResult> ActualizarCombosMasivos([FromBody] List<ArticuloComboMigracionDto> combos)
        {
            if (combos == null || !combos.Any())
                return BadRequest(new { success = false, message = "No se recibieron combos para actualizar." });

            var resultados = new List<object>();

            try
            {
                string sessionCookie = await _sapAuth.ObtenerCookieSesionAsync();
                using var client = CrearSapHttpClient();

                foreach (var combo in combos)
                {
                    try
                    {
                        var resultado = await _articleService.ActualizarArticuloComboAsync(combo, sessionCookie, client);
                        resultados.Add(new { itemCode = combo.ItemCode, status = resultado.Exito ? "OK" : "ERROR_ITEM", message = resultado.Mensaje });
                    }
                    catch (Exception exItem)
                    {
                        resultados.Add(new { itemCode = combo.ItemCode, status = "EXCEPTION", message = exItem.Message });
                    }
                }

                return Ok(new { success = true, totalProcesados = combos.Count, detalles = resultados });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Error general en la pasarela Service Layer: {ex.Message}" });
            }
        }
    }
}
