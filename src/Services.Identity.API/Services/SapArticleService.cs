using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Services.Identity.API.DTOs;

namespace Services.Identity.API.Services
{
    public class SapArticleService
    {
        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = null // OBLIGATORIO: Mantiene estrictamente PascalCase para SAP
        };

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

                // Uso de ruta relativa. El Controller pone el https://.../b1s/v1/
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

                // REGLAS: Artículos Simples
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

                // 1. Payload del Artículo Padre (Combo)
                var itemPayload = new
                {
                    ItemCode = item.ItemCode,
                    ItemName = item.ItemName,
                    ItemType = item.ItemType ?? "I",
                    ItemsGroupCode = item.ItemsGroupCode,

                    // REGLA: Combos -> Impuesto Indirecto en NO
                    WTLiable = "tYES",
                    VatLiable = "tYES",
                    IndirectTax = "tNO",

                    // REGLA: Logística de Combos
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

                // 2. Payload para la Lista de Materiales (BOM)
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
                if (!string.IsNullOrWhiteSpace(item.U_EXX_TIPOEXIS)) patchPayload["U_EXX_TIPOEXIS"] = item.U_EXX_TIPOEXIS;
                if (!string.IsNullOrWhiteSpace(item.U_EXX_TIPOUMED)) patchPayload["U_EXX_TIPOUMED"] = item.U_EXX_TIPOUMED;
                if (!string.IsNullOrWhiteSpace(item.U_EXM_PERCOM)) patchPayload["U_EXM_PERCOM"] = item.U_EXM_PERCOM;
                if (!string.IsNullOrWhiteSpace(item.U_EXM_ESTOBS)) patchPayload["U_EXM_ESTOBS"] = item.U_EXM_ESTOBS;
                if (!string.IsNullOrWhiteSpace(item.U_MKA_TINCOS)) patchPayload["U_MKA_TINCOS"] = item.U_MKA_TINCOS;

                if (item.Precios != null && item.Precios.Any())
                {
                    patchPayload["ItemPrices"] = item.Precios.Select(p => new { PriceList = p.PriceListId, Price = p.Price }).ToList();
                }

                // REGLAS: Combos (Se fuerza el NO en el Impuesto Indirecto)
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

        private void EnsureCookie(HttpClient client, string sessionCookie)
        {
            if (!client.DefaultRequestHeaders.Contains("Cookie"))
            {
                client.DefaultRequestHeaders.Add("Cookie", sessionCookie);
            }
        }
    }
}