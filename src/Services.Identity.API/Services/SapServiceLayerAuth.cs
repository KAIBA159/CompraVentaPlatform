using Microsoft.Extensions.Options;
using Services.Identity.API.DTOs;
using System.Net.Http.Json;
using System.Text.Json;

namespace Services.Identity.API.Services
{
    public class SapServiceLayerAuth
    {
        private readonly SapSettings _sapSettings;

        // REGLA CLAVE: Evita que .NET 10 convierta CompanyDB a companyDB
        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = null
        };

        public SapServiceLayerAuth(IOptions<SapSettings> sapSettings)
        {
            _sapSettings = sapSettings.Value;
        }

        public async Task<string> ObtenerCookieSesionAsync()
        {
            // Validar que las configuraciones se estén leyendo (Protección contra JSON vacío)
            if (string.IsNullOrEmpty(_sapSettings.Server))
                throw new Exception("La configuración de SAP (Server) no se ha cargado correctamente desde appsettings.json.");

            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };

            using var client = new HttpClient(handler)
            {
                BaseAddress = new Uri(_sapSettings.Server)
            };

            var loginPayload = new
            {
                CompanyDB = _sapSettings.CompanyDB,
                UserName = _sapSettings.UserName,
                Password = _sapSettings.Password
            };

            // Inyectamos _jsonOptions para forzar que el JSON respete las mayúsculas
            var response = await client.PostAsJsonAsync("Login", loginPayload, _jsonOptions);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"Error al autenticar en SAP Service Layer: {errorContent}");
            }

            if (response.Headers.TryGetValues("Set-Cookie", out var cookies))
            {
                var sessionCookie = cookies.FirstOrDefault(c => c.Contains("B1SESSION"));
                if (!string.IsNullOrEmpty(sessionCookie))
                {
                    return sessionCookie.Split(';')[0];
                }
            }

            throw new Exception("No se pudo extraer la cookie B1SESSION de la respuesta de SAP.");
        }
    }
}