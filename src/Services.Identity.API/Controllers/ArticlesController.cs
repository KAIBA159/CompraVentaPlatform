using Microsoft.AspNetCore.Mvc;
using Services.Identity.API.Services;
using Services.Identity.API.DTOs;

namespace Services.Identity.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ArticlesController : ControllerBase
    {
        private readonly SapServiceLayerAuth _sapAuth;
        private readonly SapArticleService _articleService;
        private readonly string _baseUrl = "https://192.168.1.17:50000/";

        public ArticlesController(SapServiceLayerAuth sapAuth, SapArticleService articleService)
        {
            _sapAuth = sapAuth;
            _articleService = articleService;
        }

        [HttpPost("crear-masivo-simples")]
        public async Task<IActionResult> CrearArticulosSimplesMasivos([FromBody] List<ArticuloSimpleMigracionDto> articulos)
        {
            if (articulos == null || !articulos.Any())
            {
                return BadRequest(new { success = false, message = "La lista de artículos está vacía." });
            }

            var resultados = new List<object>();

            try
            {
                string sessionCookie = await _sapAuth.ObtenerCookieSesionAsync();

                var handler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
                };
                using var client = new HttpClient(handler) { BaseAddress = new Uri(_baseUrl) };

                foreach (var item in articulos)
                {
                    try
                    {
                        var resultado = await _articleService.CrearArticuloSimpleAsync(item, sessionCookie, client);

                        if (!resultado.Exito)
                        {
                            resultados.Add(new { itemCode = item.ItemCode, status = "ERROR_ITEM", message = resultado.Mensaje });
                            continue;
                        }

                        resultados.Add(new { itemCode = item.ItemCode, status = "OK", message = "Registrado correctamente en SAP B1" });
                    }
                    catch (Exception exItem)
                    {
                        resultados.Add(new { itemCode = item.ItemCode, status = "EXCEPTION", message = exItem.Message });
                    }
                }

                return Ok(new
                {
                    success = true,
                    totalProcesados = articulos.Count,
                    detalles = resultados
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Error general en la pasarela Service Layer: {ex.Message}" });
            }
        }

        // =========================================================================
        // NUEVO ENDPOINT: ACTUALIZAR ARTÍCULOS SIMPLES MASIVOS POR LOTES
        // =========================================================================
        [HttpPost("actualizar-masivo-simples")]
        public async Task<IActionResult> ActualizarArticulosSimplesMasivos([FromBody] List<ArticuloSimpleMigracionDto> articulos)
        {
            if (articulos == null || !articulos.Any())
            {
                return BadRequest(new { success = false, message = "La lista de artículos está vacía." });
            }

            var resultados = new List<object>();

            try
            {
                // 1. Obtenemos la cookie de sesión activa de la Service Layer
                string sessionCookie = await _sapAuth.ObtenerCookieSesionAsync();

                var handler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
                };
                using var client = new HttpClient(handler) { BaseAddress = new Uri(_baseUrl) };

                foreach (var item in articulos)
                {
                    try
                    {
                        // 2. Invocamos al método de actualización individual en el servicio
                        var resultado = await _articleService.ActualizarArticuloSimpleAsync(item, sessionCookie, client);

                        if (!resultado.Exito)
                        {
                            resultados.Add(new { itemCode = item.ItemCode, status = "ERROR_ITEM", message = resultado.Mensaje });
                            continue;
                        }

                        resultados.Add(new { itemCode = item.ItemCode, status = "OK", message = "Actualizado correctamente en SAP B1" });
                    }
                    catch (Exception exItem)
                    {
                        resultados.Add(new { itemCode = item.ItemCode, status = "EXCEPTION", message = exItem.Message });
                    }
                }

                return Ok(new
                {
                    success = true,
                    totalProcesados = articulos.Count,
                    detalles = resultados
                });
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
            string sessionCookie = await _sapAuth.ObtenerCookieSesionAsync();

            var handler = new HttpClientHandler { ServerCertificateCustomValidationCallback = (m, c, ch, e) => true };
            using var client = new HttpClient(handler) { BaseAddress = new Uri(_baseUrl) };

            foreach (var combo in combos)
            {
                var resultado = await _articleService.CrearArticuloComboAsync(combo, sessionCookie, client);
                resultados.Add(new { itemCode = combo.ItemCode, status = resultado.Exito ? "OK" : "ERROR", message = resultado.Mensaje });
            }

            return Ok(new { success = true, totalProcesados = combos.Count, detalles = resultados });
        }








    }
}