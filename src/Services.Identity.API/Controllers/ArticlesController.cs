using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
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

        // ==============================================================================
        // MÉTODOS EXISTENTES: ARTÍCULOS SIMPLES Y COMBOS
        // ==============================================================================

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

        // ==============================================================================
        // MÉTODOS DE ACTUALIZACIÓN ESPECÍFICA (PATCH)
        // ==============================================================================

        [HttpPatch("update-manufacturer")]
        public async Task<IActionResult> ActualizarFabricantePaisMasivo([FromBody] List<ActualizarFabricantePaisDto> payload)
        {
            if (payload == null || !payload.Any())
                return BadRequest(new { success = false, message = "La lista de datos está vacía." });

            var resultados = new List<object>();

            try
            {
                string sessionCookie = await _sapAuth.ObtenerCookieSesionAsync();

                foreach (var item in payload)
                {
                    try
                    {
                        var mensaje = await _articleService.ActualizarFabricanteYPaisAsync(item, sessionCookie);
                        resultados.Add(new { itemCode = item.ItemCode, status = "OK", message = mensaje });
                    }
                    catch (Exception exItem)
                    {
                        resultados.Add(new { itemCode = item.ItemCode, status = "ERROR", message = exItem.Message });
                    }
                }

                return Ok(new { success = true, totalProcesados = payload.Count, detalles = resultados });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Error general: {ex.Message}" });
            }
        }

        [HttpPatch("update-price")]
        public async Task<IActionResult> ActualizarPrecioListaMasivo([FromBody] List<ActualizarPrecioListaDto> payload)
        {
            if (payload == null || !payload.Any())
                return BadRequest(new { success = false, message = "La lista de precios está vacía." });

            var resultados = new List<object>();

            try
            {
                string sessionCookie = await _sapAuth.ObtenerCookieSesionAsync();

                if (string.IsNullOrEmpty(sessionCookie))
                    return Unauthorized(new { success = false, message = "No se pudo autenticar en la Service Layer de SAP." });

                // Función auxiliar para renovar la sesión si SAP bota el error 301
                Func<Task<string>> renovarSesion = async () => await _sapAuth.ObtenerCookieSesionAsync();

                foreach (var item in payload)
                {
                    try
                    {
                        var mensaje = await _articleService.ActualizarPrecioEspecificoAsync(item, sessionCookie, renovarSesion);
                        resultados.Add(new { itemCode = item.ItemCode, status = "OK", message = mensaje });
                    }
                    catch (Exception exItem)
                    {
                        resultados.Add(new { itemCode = item.ItemCode, status = "ERROR", message = exItem.Message });
                    }
                }

                return Ok(new { success = true, totalProcesados = payload.Count, detalles = resultados });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Error general: {ex.Message}" });
            }
        }




    }
}