using System.Net.Http.Json;
using System.Text.Json; // <-- Importante para controlar el formato de mayúsculas/minúsculas
using Services.Identity.API.DTOs;

namespace Services.Identity.API.Services
{
    public class SapArticleService
    {
        private readonly string _baseUrl = "https://192.168.1.17:50000/b1s/v1/";

        public async Task<(bool Exito, string Mensaje)> CrearArticuloSimpleAsync(
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
                var itemPrices = item.Precios.Select(p => new
                {
                    PriceList = p.PriceListId,
                    Price = p.Price
                }).ToList();

                // Las propiedades DEBEN estar en PascalCase exacto para SAP B1
                var itemPayload = new
                {
                    ItemCode = item.ItemCode,
                    ItemName = item.ItemName,
                    ItemType = item.ItemType,
                    ItemsGroupCode = item.ItemsGroupCode,

                    // Inyección de los campos requeridos por el procedimiento almacenado
                    U_EXX_TIPOEXIS = item.U_EXX_TIPOEXIS, // Viajará como "01"
                    U_EXX_TIPOUMED = item.U_EXX_TIPOUMED, // Viajará como "NIU"
                    U_EXM_PERCOM = item.U_EXM_PERCOM,     // Viajará como "Y" o "N"
                    U_EXM_ESTOBS = item.U_EXM_ESTOBS,     // Viajará como "Y" o "N"
                    U_MKA_TINCOS = item.U_MKA_TINCOS,     // Viajará como "Y" o "N"

                    ItemPrices = itemPrices


                };

                // 3. Configuración para evitar que .NET convierta las mayúsculas a minúsculas
                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = null // Mantiene estrictamente ItemCode, ItemName, etc.
                };

                // 4. Aquí colocas la URL absoluta directamente en el PostAsJsonAsync
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
    }
}