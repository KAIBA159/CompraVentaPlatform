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
        // CARGA DESDE EXCEL (NUEVO ENDPOINT)
        // ==============================================================================


        [HttpPost("upload-excel-fabricante")]
        public async Task<IActionResult> UploadExcelFabricantePais(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { success = false, message = "No se proporcionó ningún archivo Excel." });

            var resultados = new List<object>();
            var dtoLista = new List<ActualizarFabricantePaisDto>();

            try
            {
                // 1. Leer el archivo Excel en memoria utilizando ClosedXML
                using (var stream = new MemoryStream())
                {
                    await file.CopyToAsync(stream);
                    using (var workbook = new XLWorkbook(stream))
                    {
                        var worksheet = workbook.Worksheet(1); // Lee la primera hoja
                        var rows = worksheet.RangeUsed().RowsUsed();
                        bool isFirstRow = true;

                        foreach (var row in rows)
                        {
                            // Omitir la fila de cabeceras
                            if (isFirstRow)
                            {
                                isFirstRow = false;
                                continue;
                            }

                            var itemCode = row.Cell(1).GetString().Trim();

                            // CORRECCIÓN: Evita procesar celdas vacías o la segunda cabecera literal
                            if (string.IsNullOrEmpty(itemCode) || itemCode.Equals("ItemCode", StringComparison.OrdinalIgnoreCase))
                                continue;




                            var dto = new ActualizarFabricantePaisDto { ItemCode = itemCode };

                            // Mapeo seguro: Solo asigna si la celda tiene un valor válido
                            var firmCodeStr = row.Cell(2).GetString().Trim();
                            if (int.TryParse(firmCodeStr, out int firmCode))
                            {
                                dto.Manufacturer = firmCode;
                            }

                            var pais = row.Cell(3).GetString().Trim();
                            if (!string.IsNullOrEmpty(pais))
                            {
                                dto.CountryOfOrigin = pais;
                            }

                            // Mapeo del nuevo campo U_MKA_CIF (Ejemplo asumiendo que está en la columna 4)
                            var cifValor = row.Cell(4).GetString().Trim();
                            if (!string.IsNullOrEmpty(cifValor))
                            {
                                dto.U_MKA_CIF = cifValor;
                            }


                            dtoLista.Add(dto);
                        }
                    }
                }

                // 2. Orquestar la actualización
                string sessionCookie = await _sapAuth.ObtenerCookieSesionAsync();
                using var client = CrearSapHttpClient();

                // Delegado mejorado: Le pasamos 'true' para forzar que SAP genere una sesión limpia
                Func<Task<string>> renovarSesion = async () =>
                {
                    return await _sapAuth.ObtenerCookieSesionAsync(forzarNuevoLogin: true);
                };



                foreach (var item in dtoLista)
                {
                    // CORRECCIÓN CABECERA EXCEL: Ignoramos si por accidente se coló la fila 2
                    if (item.ItemCode.Equals("ItemCode", StringComparison.OrdinalIgnoreCase))
                        continue;

                    try
                    {
                        var mensaje = await _articleService.ActualizarFabricanteYPaisAsync(item, sessionCookie, client, renovarSesion);
                        resultados.Add(new { itemCode = item.ItemCode, status = "OK", message = mensaje });
                    }
                    catch (Exception exItem)
                    {
                        resultados.Add(new { itemCode = item.ItemCode, status = "ERROR", message = exItem.Message });
                    }
                }

                return Ok(new { success = true, totalProcesados = dtoLista.Count, detalles = resultados });





            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Error crítico al procesar el archivo: {ex.Message}" });
            }
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

        // ==============================================================================
        // ENDPOINT: ACTUALIZAR COMBOS MASIVOS (PATCH)
        // ==============================================================================
        [HttpPost("actualizar-masivo-combos")] // React manda un POST con el JSON masivo a este endpoint de control
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
                        // Llama al método que hace PATCH dinámico (ignora nulos)
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
                return StatusCode(500, new { success = false, message = $"Error general en Service Layer: {ex.Message}" });
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
                using var client = CrearSapHttpClient();

                // Definimos el delegado para renovar la sesión en caliente
                Func<Task<string>> renovarSesion = async () =>
                {
                    // ObtenerCookieSesionAsync debe encargarse de forzar un login nuevo
                    // y no devolver una cookie en caché si esta caducó.
                    return await _sapAuth.ObtenerCookieSesionAsync();
                };

                foreach (var item in payload)
                {
                    try
                    {
                        // Agregamos renovarSesion como el cuarto argumento
                        var mensaje = await _articleService.ActualizarFabricanteYPaisAsync(item, sessionCookie, client, renovarSesion);
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