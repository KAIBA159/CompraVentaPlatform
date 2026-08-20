using System.Net.Http.Json;
using System.Text.Json;
using Services.Identity.API.DTOs;

namespace Services.Identity.API.Services
{
    public class SapArticleService
    {
        private readonly string _baseUrl = "https://192.168.1.17:50000/b1s/v1/";

        /// <summary>
        /// Crea un artículo simple en SAP Business One con sus precios múltiples y campos SUNAT.
        /// </summary>
        public async Task<(bool Exito, string Mensaje)> CrearArticuloSimpleAsync(
            ArticuloSimpleMigracionDto item,
            string sessionCookie,
            HttpClient client)
        {
            try
            {
                // 1. Asegurar la cookie de sesión en las cabeceras del cliente HTTP
                if (!client.DefaultRequestHeaders.Contains("Cookie"))
                {
                    client.DefaultRequestHeaders.Add("Cookie", sessionCookie);
                }

                // 2. Mapeo de precios múltiples hacia la estructura de la Service Layer
                var itemPrices = item.Precios?.Select(p => new
                {
                    PriceList = p.PriceListId,
                    Price = p.Price
                }).ToList() ?? new();

                // 3. Construcción del payload respetando los nombres exactos de SAP
                var itemPayload = new
                {
                    ItemCode = item.ItemCode,
                    ItemName = item.ItemName,
                    ItemType = item.ItemType ?? "I",
                    ItemsGroupCode = item.ItemsGroupCode,
                    InvntItem = item.InvntItem,      // <-- Con I mayúscula
                    SellItem = item.SellItem,        // <-- Con S mayúscula
                    PrchseItem = item.Prchselitem,   // <-- Nota el orden: PrchseItem (con 'se' antes de Item)
                    U_EXX_TIPOEXIS = item.U_EXX_TIPOEXIS,
                    U_EXX_TIPOUMED = item.U_EXX_TIPOUMED,
                    U_EXM_PERCOM = item.U_EXM_PERCOM,
                    U_EXM_ESTOBS = item.U_EXM_ESTOBS,
                    U_MKA_TINCOS = item.U_MKA_TINCOS
                };

                // 4. Configuración para evitar que .NET convierta las propiedades a minúsculas (camelCase)
                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = null // Mantiene estrictamente PascalCase (ItemCode, ItemName, etc.)
                };

                // 5. Envío de la petición POST con la URL absoluta
                string urlAbsoluta = $"{_baseUrl}Items";
                var response = await client.PostAsJsonAsync(urlAbsoluta, itemPayload, jsonOptions);

                if (response.IsSuccessStatusCode)
                {
                    return (true, "Registrado correctamente en SAP B1");
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    return (false, $"Error Service Layer: {errorContent}");
                }
            }
            catch (Exception ex)
            {
                return (false, $"Excepción interna: {ex.Message}");
            }
        }

        /// <summary>
        /// Actualiza un artículo simple existente en SAP Business One utilizando el verbo PATCH de OData.
        /// </summary>
        public async Task<(bool Exito, string Mensaje)> ActualizarArticuloSimpleAsync(
            ArticuloSimpleMigracionDto item,
            string sessionCookie,
            HttpClient client)
        {
            try
            {
                // 1. Asegurar la cookie de sesión
                if (!client.DefaultRequestHeaders.Contains("Cookie"))
                {
                    client.DefaultRequestHeaders.Add("Cookie", sessionCookie);
                }

                // 2. Mapeo de precios múltiples
                var itemPrices = item.Precios?.Select(p => new
                {
                    PriceList = p.PriceListId,
                    Price = p.Price
                }).ToList() ?? new();

                // 3. Payload con los campos modificables
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

                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = null // Mantiene PascalCase
                };

                // 4. Petición PATCH OData apuntando al recurso específico: Items('CodigoArticulo')
                string urlOData = $"{_baseUrl}Items('{item.ItemCode}')";
                var response = await client.PatchAsJsonAsync(urlOData, itemPayload, jsonOptions);

                if (response.IsSuccessStatusCode)
                {
                    return (true, "Actualizado correctamente en SAP B1");
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    return (false, $"Error Service Layer: {errorContent}");
                }
            }
            catch (Exception ex)
            {
                return (false, $"Excepción interna: {ex.Message}");
            }
        }

        public async Task<(bool Exito, string Mensaje)> CrearArticuloComboAsync(
    ArticuloComboMigracionDto item, string sessionCookie, HttpClient client)
        {
            try
            {
                if (!client.DefaultRequestHeaders.Contains("Cookie"))
                    client.DefaultRequestHeaders.Add("Cookie", sessionCookie);

                // 1. Payload del Artículo Padre con los indicadores solicitados
                var itemPayload = new
                {
                    ItemCode = item.ItemCode,
                    ItemName = item.ItemName,
                    ItemType = item.ItemType ?? "I",
                    ItemsGroupCode = item.ItemsGroupCode,
                    InvntItem = item.InvntItem,
                    SellItem = item.SellItem,
                    Prchselitem = item.Prchselitem,
                    U_EXX_TIPOEXIS = item.U_EXX_TIPOEXIS,
                    U_EXX_TIPOUMED = item.U_EXX_TIPOUMED,
                    U_EXM_PERCOM = item.U_EXM_PERCOM,
                    U_EXM_ESTOBS = item.U_EXM_ESTOBS,
                    U_MKA_TINCOS = item.U_MKA_TINCOS
                };

                var responseItem = await client.PostAsJsonAsync("b1s/v1/Items", itemPayload);
                if (!responseItem.IsSuccessStatusCode)
                {
                    return (false, $"Error al crear artículo padre: {await responseItem.Content.ReadAsStringAsync()}");
                }

                // 2. Payload para la Lista de Materiales (BOM) vía ProductTrees
                var treeLines = item.Componentes.Select(c => new
                {
                    ChildCode = c.ItemCode,
                    Quantity = c.Quantity,
                    Warehouse = "ALM01"
                }).ToList();

                var bomPayload = new
                {
                    TreeCode = item.ItemCode,
                    TreeType = item.TreeType,
                    PriceList = 1,
                    ProductTreeLines = treeLines
                };

                var responseBom = await client.PostAsJsonAsync("b1s/v1/ProductTrees", bomPayload);
                if (!responseBom.IsSuccessStatusCode)
                {
                    return (false, $"Artículo creado, but Error en Lista de Materiales: {await responseBom.Content.ReadAsStringAsync()}");
                }

                return (true, "Combo y Lista de Materiales registrados correctamente en SAP B1");
            }
            catch (Exception ex)
            {
                return (false, $"Excepción interna: {ex.Message}");
            }
        }





    }
}