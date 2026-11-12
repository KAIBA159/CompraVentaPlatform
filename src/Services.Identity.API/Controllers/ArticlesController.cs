using Microsoft.AspNetCore.Mvc;
using ClosedXML.Excel;
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
        private readonly SapConcurrencyService _sapConcurrency;

        public ArticlesController(
            SapServiceLayerAuth sapAuth,
            SapArticleService articleService,
            IOptions<SapSettings> sapSettings,
            SapConcurrencyService sapConcurrency)
        {
            _sapAuth = sapAuth;
            _articleService = articleService;
            _sapSettings = sapSettings.Value;
            _sapConcurrency = sapConcurrency;
        }

        // Handler compartido para reutilizar sockets TCP/SSL sin colisión de cabeceras entre hilos
        private HttpClientHandler CrearSapHttpHandler()
        {
            return new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };
        }

        private HttpClient CrearClienteLigero(HttpMessageHandler sharedHandler)
        {
            return new HttpClient(sharedHandler, disposeHandler: false)
            {
                BaseAddress = new Uri(_sapSettings.Server)
            };
        }

        // ==============================================================================
        // CARGA DESDE EXCEL (CONCURRENTE)
        // ==============================================================================
        [HttpPost("upload-excel-fabricante")]
        public async Task<IActionResult> UploadExcelFabricantePais(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { success = false, message = "No se proporcionó ningún archivo Excel." });

            var dtoLista = new List<ActualizarFabricantePaisDto>();

            try
            {
                using (var stream = new MemoryStream())
                {
                    await file.CopyToAsync(stream);
                    using (var workbook = new XLWorkbook(stream))
                    {
                        var worksheet = workbook.Worksheet(1);
                        var rows = worksheet.RangeUsed().RowsUsed();
                        bool isFirstRow = true;

                        foreach (var row in rows)
                        {
                            if (isFirstRow)
                            {
                                isFirstRow = false;
                                continue;
                            }

                            var itemCode = row.Cell(1).GetString().Trim();

                            if (string.IsNullOrEmpty(itemCode) || itemCode.Equals("ItemCode", StringComparison.OrdinalIgnoreCase))
                                continue;

                            var dto = new ActualizarFabricantePaisDto { ItemCode = itemCode };

                            var firmCodeStr = row.Cell(2).GetString().Trim();
                            if (int.TryParse(firmCodeStr, out int firmCode))
                                dto.Manufacturer = firmCode;

                            var pais = row.Cell(3).GetString().Trim();
                            if (!string.IsNullOrEmpty(pais))
                                dto.CountryOfOrigin = pais;

                            var cifValor = row.Cell(4).GetString().Trim();
                            if (!string.IsNullOrEmpty(cifValor))
                                dto.U_MKA_CIF = cifValor;

                            dtoLista.Add(dto);
                        }
                    }
                }

                string sessionCookie = await _sapAuth.ObtenerCookieSesionAsync();
                using var handler = CrearSapHttpHandler();
                Func<Task<string>> renovarSesion = async () => await _sapAuth.ObtenerCookieSesionAsync(forzarNuevoLogin: true);

                var resultados = await _sapConcurrency.EjecutarLoteAsync<ActualizarFabricantePaisDto, object>(dtoLista, async item =>
                {
                    try
                    {
                        using var client = CrearClienteLigero(handler);
                        var mensaje = await _articleService.ActualizarFabricanteYPaisAsync(item, sessionCookie, client, renovarSesion);
                        return new { itemCode = item.ItemCode, status = "OK", message = mensaje };
                    }
                    catch (Exception exItem)
                    {
                        return new { itemCode = item.ItemCode, status = "ERROR", message = exItem.Message };
                    }
                });

                return Ok(new
                {
                    success = true,
                    hilosConfigurados = _sapConcurrency.MaxConcurrency,
                    totalProcesados = dtoLista.Count,
                    detalles = resultados
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Error crítico al procesar el archivo: {ex.Message}" });
            }
        }

        // ==============================================================================
        // ARTÍCULOS SIMPLES (CONCURRENTES)
        // ==============================================================================
        [HttpPost("crear-masivo-simples")]
        public async Task<IActionResult> CrearArticulosSimplesMasivos([FromBody] List<ArticuloSimpleMigracionDto> articulos)
        {
            if (articulos == null || !articulos.Any())
                return BadRequest(new { success = false, message = "La lista de artículos está vacía." });

            try
            {
                string sessionCookie = await _sapAuth.ObtenerCookieSesionAsync();
                using var handler = CrearSapHttpHandler();

                var resultados = await _sapConcurrency.EjecutarLoteAsync<ArticuloSimpleMigracionDto, object>(articulos, async item =>
                {
                    try
                    {
                        using var client = CrearClienteLigero(handler);
                        var resultado = await _articleService.CrearArticuloSimpleAsync(item, sessionCookie, client);
                        return new
                        {
                            itemCode = item.ItemCode,
                            status = resultado.Exito ? "OK" : "ERROR_ITEM",
                            message = resultado.Exito ? "Registrado correctamente en SAP B1" : resultado.Mensaje
                        };
                    }
                    catch (Exception exItem)
                    {
                        return new { itemCode = item.ItemCode, status = "EXCEPTION", message = exItem.Message };
                    }
                });

                return Ok(new
                {
                    success = true,
                    hilosConfigurados = _sapConcurrency.MaxConcurrency,
                    totalProcesados = articulos.Count,
                    detalles = resultados
                });
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

            try
            {
                string sessionCookie = await _sapAuth.ObtenerCookieSesionAsync();
                using var handler = CrearSapHttpHandler();

                var resultados = await _sapConcurrency.EjecutarLoteAsync<ArticuloSimpleMigracionDto, object>(articulos, async item =>
                {
                    try
                    {
                        using var client = CrearClienteLigero(handler);
                        var resultado = await _articleService.ActualizarArticuloSimpleAsync(item, sessionCookie, client);
                        return new
                        {
                            itemCode = item.ItemCode,
                            status = resultado.Exito ? "OK" : "ERROR_ITEM",
                            message = resultado.Exito ? "Actualizado correctamente en SAP B1" : resultado.Mensaje
                        };
                    }
                    catch (Exception exItem)
                    {
                        return new { itemCode = item.ItemCode, status = "EXCEPTION", message = exItem.Message };
                    }
                });

                return Ok(new
                {
                    success = true,
                    hilosConfigurados = _sapConcurrency.MaxConcurrency,
                    totalProcesados = articulos.Count,
                    detalles = resultados
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Error general en la pasarela Service Layer: {ex.Message}" });
            }
        }

        // ==============================================================================
        // COMBOS (CONCURRENTES)
        // ==============================================================================
        [HttpPost("crear-masivo-combos")]
        public async Task<IActionResult> CrearCombosMasivos([FromBody] List<ArticuloComboMigracionDto> combos)
        {
            if (combos == null || !combos.Any())
                return BadRequest(new { success = false, message = "No se recibieron combos para procesar." });

            try
            {
                string sessionCookie = await _sapAuth.ObtenerCookieSesionAsync();
                using var handler = CrearSapHttpHandler();

                var resultados = await _sapConcurrency.EjecutarLoteAsync<ArticuloComboMigracionDto, object>(combos, async combo =>
                {
                    try
                    {
                        using var client = CrearClienteLigero(handler);
                        var resultado = await _articleService.CrearArticuloComboAsync(combo, sessionCookie, client);
                        return new { itemCode = combo.ItemCode, status = resultado.Exito ? "OK" : "ERROR_ITEM", message = resultado.Mensaje };
                    }
                    catch (Exception exItem)
                    {
                        return new { itemCode = combo.ItemCode, status = "EXCEPTION", message = exItem.Message };
                    }
                });

                return Ok(new
                {
                    success = true,
                    hilosConfigurados = _sapConcurrency.MaxConcurrency,
                    totalProcesados = combos.Count,
                    detalles = resultados
                });
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

            try
            {
                string sessionCookie = await _sapAuth.ObtenerCookieSesionAsync();
                using var handler = CrearSapHttpHandler();

                var resultados = await _sapConcurrency.EjecutarLoteAsync<ArticuloComboMigracionDto, object>(combos, async combo =>
                {
                    try
                    {
                        using var client = CrearClienteLigero(handler);
                        var resultado = await _articleService.ActualizarArticuloComboAsync(combo, sessionCookie, client);
                        return new { itemCode = combo.ItemCode, status = resultado.Exito ? "OK" : "ERROR_ITEM", message = resultado.Mensaje };
                    }
                    catch (Exception exItem)
                    {
                        return new { itemCode = combo.ItemCode, status = "EXCEPTION", message = exItem.Message };
                    }
                });

                return Ok(new
                {
                    success = true,
                    hilosConfigurados = _sapConcurrency.MaxConcurrency,
                    totalProcesados = combos.Count,
                    detalles = resultados
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Error general en Service Layer: {ex.Message}" });
            }
        }

        // ==============================================================================
        // ACTUALIZACIONES ESPECÍFICAS PATCH (CONCURRENTES)
        // ==============================================================================
        [HttpPatch("update-manufacturer")]
        public async Task<IActionResult> ActualizarFabricantePaisMasivo([FromBody] List<ActualizarFabricantePaisDto> payload)
        {
            if (payload == null || !payload.Any())
                return BadRequest(new { success = false, message = "La lista de datos está vacía." });

            try
            {
                string sessionCookie = await _sapAuth.ObtenerCookieSesionAsync();
                using var handler = CrearSapHttpHandler();
                Func<Task<string>> renovarSesion = async () => await _sapAuth.ObtenerCookieSesionAsync(forzarNuevoLogin: true);

                var resultados = await _sapConcurrency.EjecutarLoteAsync<ActualizarFabricantePaisDto, object>(payload, async item =>
                {
                    try
                    {
                        using var client = CrearClienteLigero(handler);
                        var mensaje = await _articleService.ActualizarFabricanteYPaisAsync(item, sessionCookie, client, renovarSesion);
                        return new { itemCode = item.ItemCode, status = "OK", message = mensaje };
                    }
                    catch (Exception exItem)
                    {
                        return new { itemCode = item.ItemCode, status = "ERROR", message = exItem.Message };
                    }
                });

                return Ok(new
                {
                    success = true,
                    hilosConfigurados = _sapConcurrency.MaxConcurrency,
                    totalProcesados = payload.Count,
                    detalles = resultados
                });
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

            try
            {
                string sessionCookie = await _sapAuth.ObtenerCookieSesionAsync();

                if (string.IsNullOrEmpty(sessionCookie))
                    return Unauthorized(new { success = false, message = "No se pudo autenticar en la Service Layer de SAP." });

                Func<Task<string>> renovarSesion = async () => await _sapAuth.ObtenerCookieSesionAsync(forzarNuevoLogin: true);

                var resultados = await _sapConcurrency.EjecutarLoteAsync<ActualizarPrecioListaDto, object>(payload, async item =>
                {
                    try
                    {
                        var mensaje = await _articleService.ActualizarPrecioEspecificoAsync(item, sessionCookie, renovarSesion);
                        return new { itemCode = item.ItemCode, status = "OK", message = mensaje };
                    }
                    catch (Exception exItem)
                    {
                        return new { itemCode = item.ItemCode, status = "ERROR", message = exItem.Message };
                    }
                });

                return Ok(new
                {
                    success = true,
                    hilosConfigurados = _sapConcurrency.MaxConcurrency,
                    totalProcesados = payload.Count,
                    detalles = resultados
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Error general: {ex.Message}" });
            }
        }
    }
}