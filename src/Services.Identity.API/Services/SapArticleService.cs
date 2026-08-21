using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Services.Identity.API.DTOs;

namespace Services.Identity.API.Services
{
    public class SapArticleService
    {
        // NOTA: Como el HttpClient ya tiene BaseAddress en el controlador, 
        // solo necesitamos pasar el nombre del recurso (ej: "Items" o "ProductTrees")

        private readonly string _baseUrl = "https://192.168.1.17:50000/b1s/v1/";

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

                var itemPrices = item.Precios?.Select(p => new { PriceList = p.PriceListId, Price = p.Price }).ToList() ?? new();

                var itemPayload = new
                {
                    ItemName = item.ItemName,
                    ItemsGroupCode = item.ItemsGroupCode,
                    U_EXX_TIPOEXIS = item.U_EXX_TIPOEXIS,
                    U_EXX_TIPOUMED = item.U_EXX_TIPOUMED,
                    U_EXM_PERCOM = item.U_EXM_PERCOM,
                    U_EXM_ESTOBS = item.U_EXM_ESTOBS,
                    U_MKA_TINCOS = item.U_MKA_TINCOS,
                    ItemPrices = itemPrices
                };

                // Uso de StringContent para PATCH, más seguro en versiones variadas de .NET
                var content = new StringContent(JsonSerializer.Serialize(itemPayload, _jsonOptions), Encoding.UTF8, "application/json");
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
        // 3. CREAR ARTÍCULO COMBO + LISTA DE MATERIALES (BOM)
        // ==============================================================================
        /*public async Task<(bool Exito, string Mensaje)> CrearArticuloComboAsync(
            ArticuloComboMigracionDto item, string sessionCookie, HttpClient client)
        {
            try
            {
                EnsureCookie(client, sessionCookie);

                // A. Crear Cabecera del Combo
                var itemPrices = item.Precios?.Select(p => new { PriceList = p.PriceListId, Price = p.Price }).ToList() ?? new();

                var itemPayload = new
                {
                    ItemCode = item.ItemCode,
                    ItemName = item.ItemName,
                    ItemType = item.ItemType ?? "I",
                    ItemsGroupCode = item.ItemsGroupCode,
                    InvntItem = item.InvntItem,         // "N"
                    SellItem = item.SellItem,           // "Y"
                    PrchseItem = item.Prchselitem,      // "N" (Ortografía exacta de SAP)
                    U_EXX_TIPOEXIS = item.U_EXX_TIPOEXIS,
                    U_EXX_TIPOUMED = item.U_EXX_TIPOUMED,
                    U_EXM_PERCOM = item.U_EXM_PERCOM,
                    U_EXM_ESTOBS = item.U_EXM_ESTOBS,
                    U_MKA_TINCOS = item.U_MKA_TINCOS,
                    ItemPrices = itemPrices
                };

                var responseItem = await client.PostAsJsonAsync("Items", itemPayload, _jsonOptions);
                if (!responseItem.IsSuccessStatusCode)
                    return (false, $"Error al crear artículo padre (Combo): {await responseItem.Content.ReadAsStringAsync()}");

                // B. Crear Lista de Materiales (BOM)
                var treeLines = item.Componentes.Select(c => new
                {
                    ChildCode = c.ItemCode,
                    Quantity = c.Quantity,
                    Warehouse = "ALM01"
                }).ToList();

                var bomPayload = new
                {
                    TreeCode = item.ItemCode,
                    TreeType = item.TreeType ?? "iSales", // iSales = Conjunto
                    PriceList = 1,
                    ProductTreeLines = treeLines
                };

                var responseBom = await client.PostAsJsonAsync("ProductTrees", bomPayload, _jsonOptions);
                if (!responseBom.IsSuccessStatusCode)
                    return (false, $"Padre creado, pero falló la Lista de Materiales: {await responseBom.Content.ReadAsStringAsync()}");

                return (true, "Combo y Lista de Materiales creados correctamente en SAP B1");
            }
            catch (Exception ex)
            {
                return (false, $"Excepción interna: {ex.Message}");
            }
        }*/


        public async Task<(bool Exito, string Mensaje)> CrearArticuloComboAsync(
    ArticuloComboMigracionDto item, string sessionCookie, HttpClient client)
        {
            try
            {
                if (!client.DefaultRequestHeaders.Contains("Cookie"))
                    client.DefaultRequestHeaders.Add("Cookie", sessionCookie);

                var itemPrices = item.Precios?.Select(p => new
                {
                    PriceList = p.PriceListId,
                    Price = p.Price
                }).ToList() ?? new();

                // 1. Payload del Artículo Padre (Combo)
                var itemPayload = new
                {
                    ItemCode = item.ItemCode,
                    ItemName = item.ItemName,
                    ItemType = item.ItemType ?? "I",
                    ItemsGroupCode = item.ItemsGroupCode,

                    // Nombres exactos exigidos por la Service Layer
                    // + Mapeo seguro y estricto hacia BoYesNoEnum
                    InventoryItem = item.InvntItem == "Y" ? "tYES" : "tNO",
                    SalesItem = item.SellItem == "Y" ? "tYES" : "tNO",
                    PurchaseItem = item.Prchselitem == "Y" ? "tYES" : "tNO",

                    U_EXX_TIPOEXIS = item.U_EXX_TIPOEXIS,
                    U_EXX_TIPOUMED = item.U_EXX_TIPOUMED,
                    U_EXM_PERCOM = item.U_EXM_PERCOM,
                    U_EXM_ESTOBS = item.U_EXM_ESTOBS,
                    U_MKA_TINCOS = item.U_MKA_TINCOS,
                    ItemPrices = itemPrices
                };

                var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = null };

                // SOLUCIÓN 1: Forzamos la URL absoluta concatenando _baseUrl
                string urlItems = $"{_baseUrl}Items";
                var responseItem = await client.PostAsJsonAsync(urlItems, itemPayload, jsonOptions);

                if (!responseItem.IsSuccessStatusCode)
                {
                    return (false, $"Error al crear artículo padre: {await responseItem.Content.ReadAsStringAsync()}");
                }

                // 2. Payload para la Lista de Materiales (BOM)
                var treeLines = item.Componentes.Select(c => new
                {
                    ItemCode = c.ItemCode,  // ✅ SAP Service Layer exige "ItemCode" para las líneas del BOM
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

                // SOLUCIÓN 2: URL absoluta para la entidad ProductTrees
                string urlBom = $"{_baseUrl}ProductTrees";
                var responseBom = await client.PostAsJsonAsync(urlBom, bomPayload, jsonOptions);

                if (!responseBom.IsSuccessStatusCode)
                {
                    return (false, $"Artículo creado, pero Error en Lista de Materiales: {await responseBom.Content.ReadAsStringAsync()}");
                }

                return (true, "Combo y Lista de Materiales registrados correctamente en SAP B1");
            }
            catch (Exception ex)
            {
                return (false, $"Excepción interna: {ex.Message}");
            }
        }




        // ==============================================================================
        // 4. ACTUALIZAR ARTÍCULO COMBO + LISTA DE MATERIALES (PATCH)
        // ==============================================================================
        public async Task<(bool Exito, string Mensaje)> ActualizarArticuloComboAsync(
            ArticuloComboMigracionDto item, string sessionCookie, HttpClient client)
        {
            try
            {
                EnsureCookie(client, sessionCookie);

                // A. Actualizar Cabecera del Combo (Items)
                var itemPrices = item.Precios?.Select(p => new { PriceList = p.PriceListId, Price = p.Price }).ToList() ?? new();

                var itemPayload = new
                {
                    ItemName = item.ItemName,
                    ItemsGroupCode = item.ItemsGroupCode,
                    InvntItem = item.InvntItem,
                    SellItem = item.SellItem,
                    PrchseItem = item.Prchselitem,
                    U_EXX_TIPOEXIS = item.U_EXX_TIPOEXIS,
                    U_EXX_TIPOUMED = item.U_EXX_TIPOUMED,
                    U_EXM_PERCOM = item.U_EXM_PERCOM,
                    U_EXM_ESTOBS = item.U_EXM_ESTOBS,
                    U_MKA_TINCOS = item.U_MKA_TINCOS,
                    ItemPrices = itemPrices
                };

                var contentItem = new StringContent(JsonSerializer.Serialize(itemPayload, _jsonOptions), Encoding.UTF8, "application/json");
                var responseItem = await client.PatchAsync($"Items('{item.ItemCode}')", contentItem);

                if (!responseItem.IsSuccessStatusCode)
                    return (false, $"Error al actualizar artículo padre (Combo): {await responseItem.Content.ReadAsStringAsync()}");

                // B. Actualizar Lista de Materiales (ProductTrees)
                // En Service Layer, al mandar ProductTreeLines en un PATCH, SAP reemplaza/actualiza las líneas enviadas.
                var treeLines = item.Componentes.Select(c => new
                {
                    ChildCode = c.ItemCode,
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

                return (true, "Combo y Lista de Materiales actualizados correctamente en SAP B1");
            }
            catch (Exception ex)
            {
                return (false, $"Excepción interna: {ex.Message}");
            }
        }

        // ==============================================================================
        // MÉTODO AUXILIAR
        // ==============================================================================
        private void EnsureCookie(HttpClient client, string sessionCookie)
        {
            if (!client.DefaultRequestHeaders.Contains("Cookie"))
            {
                client.DefaultRequestHeaders.Add("Cookie", sessionCookie);
            }
        }
    }
}