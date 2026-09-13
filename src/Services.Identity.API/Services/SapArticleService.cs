using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Services.Identity.API.DTOs;

namespace Services.Identity.API.Services
{
    public class SapArticleService
    {
        // 1. Añadimos la variable privada para almacenar la configuración de SAP
        private readonly SapSettings _sapSettings;

        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = null // OBLIGATORIO: Mantiene estrictamente PascalCase para SAP
        };

        // 2. Añadimos el constructor para inyectar IOptions<SapSettings>
        public SapArticleService(IOptions<SapSettings> sapSettings)
        {
            _sapSettings = sapSettings.Value;
        }

        // ==============================================================================
        // 1. CREAR ARTÍCULO SIMPLE
        // ==============================================================================
        public async Task<(bool Exito, string Mensaje)> CrearArticuloSimpleAsync(
            ArticuloSimpleMigracionDto item, string sessionCookie, HttpClient client)
        {
            try
            {
                EnsureCookie(client, sessionCookie);

                var itemPrices = item.Precios?.Select(p => new { PriceList = p.PriceListId, Price = p.Price }).ToList() ?? new();

                var itemPayload = new
                {
                    ItemCode = item.ItemCode,
                    ItemName = item.ItemName,
                    ItemType = item.ItemType ?? "I",
                    ItemsGroupCode = item.ItemsGroupCode,

                    // REGLA: Artículos Simples -> Impuestos SIEMPRE ACTIVOS
                    WTLiable = "tYES",
                    VatLiable = "tYES",
                    IndirectTax = "tYES",

                    // REGLA: Artículos Simples -> Logística SIEMPRE ACTIVA
                    InventoryItem = "tYES",
                    SalesItem = "tYES",
                    PurchaseItem = "tYES",

                    U_EXX_TIPOEXIS = item.U_EXX_TIPOEXIS,
                    U_EXX_TIPOUMED = item.U_EXX_TIPOUMED,
                    U_EXM_PERCOM = item.U_EXM_PERCOM,
                    U_EXM_ESTOBS = item.U_EXM_ESTOBS,
                    U_MKA_TINCOS = item.U_MKA_TINCOS,
                    ItemPrices = itemPrices
                };

                var response = await client.PostAsJsonAsync("Items", itemPayload, _jsonOptions);

                if (!response.IsSuccessStatusCode)
                    return (false, $"Error al crear artículo simple: {await response.Content.ReadAsStringAsync()}");

                return (true, "Artículo simple registrado correctamente en SAP B1");
            }
            catch (Exception ex)
            {
                return (false, $"Excepción interna: {ex.Message}");
            }
        }

        // ==============================================================================
        // 2. ACTUALIZAR ARTÍCULO SIMPLE (PATCH)
        // ==============================================================================
        public async Task<(bool Exito, string Mensaje)> ActualizarArticuloSimpleAsync(
            ArticuloSimpleMigracionDto item, string sessionCookie, HttpClient client)
        {
            try
            {
                EnsureCookie(client, sessionCookie);

                var patchPayload = new Dictionary<string, object>();

                if (!string.IsNullOrWhiteSpace(item.ItemName)) patchPayload["ItemName"] = item.ItemName;
                if (item.ItemsGroupCode > 0) patchPayload["ItemsGroupCode"] = item.ItemsGroupCode;
                if (!string.IsNullOrWhiteSpace(item.U_EXX_TIPOEXIS)) patchPayload["U_EXX_TIPOEXIS"] = item.U_EXX_TIPOEXIS;
                if (!string.IsNullOrWhiteSpace(item.U_EXX_TIPOUMED)) patchPayload["U_EXX_TIPOUMED"] = item.U_EXX_TIPOUMED;
                if (!string.IsNullOrWhiteSpace(item.U_EXM_PERCOM)) patchPayload["U_EXM_PERCOM"] = item.U_EXM_PERCOM;
                if (!string.IsNullOrWhiteSpace(item.U_EXM_ESTOBS)) patchPayload["U_EXM_ESTOBS"] = item.U_EXM_ESTOBS;
                if (!string.IsNullOrWhiteSpace(item.U_MKA_TINCOS)) patchPayload["U_MKA_TINCOS"] = item.U_MKA_TINCOS;

                if (item.Precios != null && item.Precios.Any())
                {
                    patchPayload["ItemPrices"] = item.Precios.Select(p => new { PriceList = p.PriceListId, Price = p.Price }).ToList();
                }

                patchPayload["WTLiable"] = "tYES";
                patchPayload["VatLiable"] = "tYES";
                patchPayload["IndirectTax"] = "tYES";
                patchPayload["InventoryItem"] = "tYES";
                patchPayload["SalesItem"] = "tYES";
                patchPayload["PurchaseItem"] = "tYES";

                var content = new StringContent(JsonSerializer.Serialize(patchPayload, _jsonOptions), Encoding.UTF8, "application/json");
                var response = await client.PatchAsync($"Items('{item.ItemCode}')", content);

                if (!response.IsSuccessStatusCode)
                    return (false, $"Error al actualizar artículo simple: {await response.Content.ReadAsStringAsync()}");

                return (true, "Artículo simple actualizado correctamente en SAP B1");
            }
            catch (Exception ex)
            {
                return (false, $"Excepción interna: {ex.Message}");
            }
        }

        // ==============================================================================
        // 3. CREAR ARTÍCULO COMBO + BOM
        // ==============================================================================
        public async Task<(bool Exito, string Mensaje)> CrearArticuloComboAsync(
            ArticuloComboMigracionDto item, string sessionCookie, HttpClient client)
        {
            try
            {
                EnsureCookie(client, sessionCookie);

                var itemPrices = item.Precios?.Select(p => new { PriceList = p.PriceListId, Price = p.Price }).ToList() ?? new();

                var itemPayload = new
                {
                    ItemCode = item.ItemCode,
                    ItemName = item.ItemName,
                    ItemType = item.ItemType ?? "I",
                    ItemsGroupCode = item.ItemsGroupCode,


                    // --- NUEVOS CAMPOS INYECTADOS AQUÍ ---
                    Valid = item.Valid,
                    ValidTo = item.ValidTo,
                    // -------------------------------------


                    WTLiable = "tYES",
                    VatLiable = "tYES",
                    IndirectTax = "tNO",

                    InventoryItem = "tNO",
                    SalesItem = "tYES",
                    PurchaseItem = "tNO",

                    U_EXX_TIPOEXIS = item.U_EXX_TIPOEXIS,
                    U_EXX_TIPOUMED = item.U_EXX_TIPOUMED,
                    U_EXM_PERCOM = item.U_EXM_PERCOM,
                    U_EXM_ESTOBS = item.U_EXM_ESTOBS,
                    U_MKA_TINCOS = item.U_MKA_TINCOS,
                    ItemPrices = itemPrices
                };

                var responseItem = await client.PostAsJsonAsync("Items", itemPayload, _jsonOptions);

                if (!responseItem.IsSuccessStatusCode)
                    return (false, $"Error al crear artículo padre: {await responseItem.Content.ReadAsStringAsync()}");

                var treeLines = item.Componentes.Select(c => new
                {
                    ItemCode = c.ItemCode,
                    Quantity = c.Quantity,
                    Warehouse = "ALM01"
                }).ToList();

                var bomPayload = new
                {
                    TreeCode = item.ItemCode,
                    TreeType = item.TreeType ?? "iSales",
                    PriceList = 1,
                    ProductTreeLines = treeLines
                };

                var responseBom = await client.PostAsJsonAsync("ProductTrees", bomPayload, _jsonOptions);

                if (!responseBom.IsSuccessStatusCode)
                    return (false, $"Artículo creado, pero Error en Lista de Materiales: {await responseBom.Content.ReadAsStringAsync()}");

                return (true, "Combo y Lista de Materiales registrados correctamente en SAP B1");
            }
            catch (Exception ex)
            {
                return (false, $"Excepción interna: {ex.Message}");
            }
        }

        // ==============================================================================
        // 4. ACTUALIZAR ARTÍCULO COMBO + BOM (PATCH)
        // ==============================================================================
        public async Task<(bool Exito, string Mensaje)> ActualizarArticuloComboAsync(
            ArticuloComboMigracionDto item, string sessionCookie, HttpClient client)
        {
            try
            {
                EnsureCookie(client, sessionCookie);

                var patchPayload = new Dictionary<string, object>();

                if (!string.IsNullOrWhiteSpace(item.ItemName)) patchPayload["ItemName"] = item.ItemName;
                if (item.ItemsGroupCode > 0) patchPayload["ItemsGroupCode"] = item.ItemsGroupCode;

                // --- NUEVOS CAMPOS INYECTADOS AQUÍ ---
                if (!string.IsNullOrWhiteSpace(item.Valid)) patchPayload["Valid"] = item.Valid;
                if (!string.IsNullOrWhiteSpace(item.ValidTo)) patchPayload["ValidTo"] = item.ValidTo;
                // -------------------------------------

                if (!string.IsNullOrWhiteSpace(item.U_EXX_TIPOEXIS)) patchPayload["U_EXX_TIPOEXIS"] = item.U_EXX_TIPOEXIS;
                if (!string.IsNullOrWhiteSpace(item.U_EXX_TIPOUMED)) patchPayload["U_EXX_TIPOUMED"] = item.U_EXX_TIPOUMED;
                if (!string.IsNullOrWhiteSpace(item.U_EXM_PERCOM)) patchPayload["U_EXM_PERCOM"] = item.U_EXM_PERCOM;
                if (!string.IsNullOrWhiteSpace(item.U_EXM_ESTOBS)) patchPayload["U_EXM_ESTOBS"] = item.U_EXM_ESTOBS;
                if (!string.IsNullOrWhiteSpace(item.U_MKA_TINCOS)) patchPayload["U_MKA_TINCOS"] = item.U_MKA_TINCOS;

                if (item.Precios != null && item.Precios.Any())
                {
                    patchPayload["ItemPrices"] = item.Precios.Select(p => new { PriceList = p.PriceListId, Price = p.Price }).ToList();
                }

                patchPayload["WTLiable"] = "tYES";
                patchPayload["VatLiable"] = "tYES";
                patchPayload["IndirectTax"] = "tNO";
                patchPayload["InventoryItem"] = "tNO";
                patchPayload["SalesItem"] = "tYES";
                patchPayload["PurchaseItem"] = "tNO";

                var contentItem = new StringContent(JsonSerializer.Serialize(patchPayload, _jsonOptions), Encoding.UTF8, "application/json");
                var responseItem = await client.PatchAsync($"Items('{item.ItemCode}')", contentItem);

                if (!responseItem.IsSuccessStatusCode)
                    return (false, $"Error al actualizar artículo padre (Combo): {await responseItem.Content.ReadAsStringAsync()}");

                if (item.Componentes != null && item.Componentes.Any())
                {
                    var treeLines = item.Componentes.Select(c => new
                    {
                        ItemCode = c.ItemCode,
                        Quantity = c.Quantity,
                        Warehouse = "ALM01"
                    }).ToList();

                    var bomPayload = new
                    {
                        TreeType = item.TreeType ?? "iSales",
                        PriceList = 1,
                        ProductTreeLines = treeLines
                    };

                    var contentBom = new StringContent(JsonSerializer.Serialize(bomPayload, _jsonOptions), Encoding.UTF8, "application/json");
                    var responseBom = await client.PatchAsync($"ProductTrees('{item.ItemCode}')", contentBom);

                    if (!responseBom.IsSuccessStatusCode)
                        return (false, $"Padre actualizado, pero falló actualizar la Lista de Materiales: {await responseBom.Content.ReadAsStringAsync()}");
                }

                return (true, "Combo y Lista de Materiales actualizados correctamente en SAP B1");
            }
            catch (Exception ex)
            {
                return (false, $"Excepción interna: {ex.Message}");
            }
        }

        // ==============================================================================
        // 5. ACTUALIZAR FABRICANTE Y PAÍS DE ORIGEN
        // ==============================================================================
        public async Task<string> ActualizarFabricanteYPaisAsync(ActualizarFabricantePaisDto dto, string sessionCookie)
        {
            var updatePayload = new
            {
                Manufacturer = dto.Manufacturer,
                ItemIntrastatExtension = new
                {
                    ItemCode = dto.ItemCode, // <-- OBLIGATORIO: Llave primaria en el sub-objeto
                    CountryOfOrigin = dto.CountryOfOrigin
                }
            };

            // ... (resto del código HttpClient sin modificaciones)
            var handler = new HttpClientHandler { ServerCertificateCustomValidationCallback = (m, c, ch, e) => true };
            using var client = new HttpClient(handler);
            client.BaseAddress = new Uri(_sapSettings.Server);
            client.DefaultRequestHeaders.Add("Cookie", $"B1SESSION={sessionCookie}");

            var response = await client.PatchAsJsonAsync($"Items('{dto.ItemCode}')", updatePayload, _jsonOptions);



            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"Fallo al actualizar {dto.ItemCode}: {errorContent}");
            }

            return $"El artículo {dto.ItemCode} fue actualizado correctamente.";
        }


        public async Task<string> ActualizarPrecioEspecificoAsync(ActualizarPrecioListaDto dto, string sessionCookie, Func<Task<string>> renovarSesionFunc = null)
        {
            if (dto.Price <= 0)
                throw new Exception($"El precio para el artículo {dto.ItemCode} es 0 o inválido. Revisa el Excel.");

            var handler = new HttpClientHandler { ServerCertificateCustomValidationCallback = (m, c, ch, e) => true };
            using var client = new HttpClient(handler) { BaseAddress = new Uri(_sapSettings.Server) };
            client.DefaultRequestHeaders.Add("Cookie", sessionCookie);

            var itemCodeSeguro = Uri.EscapeDataString(dto.ItemCode);
            var response = await client.GetAsync($"Items('{itemCodeSeguro}')?$select=ItemCode,ItemPrices");

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();

                if (errorContent.Contains("301") && renovarSesionFunc != null)
                {
                    sessionCookie = await renovarSesionFunc();
                    client.DefaultRequestHeaders.Remove("Cookie");
                    client.DefaultRequestHeaders.Add("Cookie", sessionCookie);

                    response = await client.GetAsync($"Items('{itemCodeSeguro}')?$select=ItemCode,ItemPrices");
                    if (!response.IsSuccessStatusCode)
                    {
                        errorContent = await response.Content.ReadAsStringAsync();
                        throw new Exception($"Fallo GET SAP tras reautenticar ({response.StatusCode}): {errorContent}");
                    }
                }
                else
                {
                    throw new Exception($"Fallo GET SAP ({response.StatusCode}): {errorContent}");
                }
            }

            var itemData = await response.Content.ReadFromJsonAsync<JsonElement>();

            JsonElement itemPricesElement = default;
            bool foundPrices = false;

            foreach (var prop in itemData.EnumerateObject())
            {
                if (prop.Name.Equals("ItemPrices", StringComparison.OrdinalIgnoreCase))
                {
                    itemPricesElement = prop.Value;
                    foundPrices = true;
                    break;
                }
            }

            int realLineNum = -1;

            if (foundPrices && itemPricesElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var priceRow in itemPricesElement.EnumerateArray())
                {
                    int priceListVal = -1;
                    int lineNumVal = -1;

                    foreach (var rowProp in priceRow.EnumerateObject())
                    {
                        if (rowProp.Name.Equals("PriceList", StringComparison.OrdinalIgnoreCase))
                            priceListVal = rowProp.Value.GetInt32();

                        if (rowProp.Name.Equals("LineNum", StringComparison.OrdinalIgnoreCase))
                            lineNumVal = rowProp.Value.GetInt32();
                    }

                    if (priceListVal == dto.PriceListId)
                    {
                        realLineNum = lineNumVal;
                        break;
                    }
                }
            }

            // PAYLOAD CONDICIONAL: Si existe LineNum se envía; si no, se omite para forzar la creación en SAP
            object updatePayload;
            if (realLineNum != -1)
            {
                updatePayload = new
                {
                    ItemPrices = new[]
                    {
                new
                {
                    LineNum = realLineNum,
                    PriceList = dto.PriceListId,
                    Price = dto.Price,
                    Currency = dto.Currency
                }
            }
                };
            }
            else
            {
                updatePayload = new
                {
                    ItemPrices = new[]
                    {
                new
                {
                    PriceList = dto.PriceListId,
                    Price = dto.Price,
                    Currency = dto.Currency
                }
            }
                };
            }

            var patchResponse = await client.PatchAsJsonAsync($"Items('{itemCodeSeguro}')", updatePayload, _jsonOptions);

            if (!patchResponse.IsSuccessStatusCode)
            {
                var patchError = await patchResponse.Content.ReadAsStringAsync();
                throw new Exception($"Fallo interno en SAP al guardar el precio: {patchError}");
            }

            return $"Precio actualizado exitosamente a {dto.Currency} {dto.Price}";
        }









        private void EnsureCookie(HttpClient client, string sessionCookie)
        {
            if (!client.DefaultRequestHeaders.Contains("Cookie"))
            {
                client.DefaultRequestHeaders.Add("Cookie", sessionCookie);
            }
        }
    }
}