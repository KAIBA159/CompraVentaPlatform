using Microsoft.Extensions.Options;
using Services.Identity.API.DTOs;
using System.Net.Http.Json;
using System.Text.Json;

namespace Services.Identity.API.Services
{
    public class SapServiceLayerAuth
    {
        private readonly SapSettings _sapSettings;
        // Caché en memoria para evitar hacer múltiples logins a SAP
        private static string _sesionCache = string.Empty;

        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = null
        };

        public SapServiceLayerAuth(IOptions<SapSettings> sapSettings)
        {
            _sapSettings = sapSettings.Value;
        }

        // Agregamos el flag para forzar un nuevo login cuando SAP arroja 301
        public async Task<string> ObtenerCookieSesionAsync(bool forzarNuevoLogin = false)
        {
            if (string.IsNullOrEmpty(_sapSettings.Server))
                throw new Exception("La configuración de SAP (Server) no se ha cargado.");

            // Retornamos la sesión en caché si aún es válida y no se nos obligó a renovar
            if (!forzarNuevoLogin && !string.IsNullOrEmpty(_sesionCache))
            {
                return _sesionCache;
            }

            var handler = new HttpClientHandler { ServerCertificateCustomValidationCallback = (m, c, ch, e) => true };
            using var client = new HttpClient(handler) { BaseAddress = new Uri(_sapSettings.Server) };

            var loginPayload = new
            {
                CompanyDB = _sapSettings.CompanyDB,
                UserName = _sapSettings.UserName,
                Password = _sapSettings.Password
            };

            var response = await client.PostAsJsonAsync("Login", loginPayload, _jsonOptions);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"Error al autenticar: {errorContent}");
            }

            if (response.Headers.TryGetValues("Set-Cookie", out var cookies))
            {
                // Extraemos TODAS las cookies (B1SESSION y ROUTEID si aplica) y las unimos
                var sessionCookies = cookies.Select(c => c.Split(';')[0]).ToList();
                _sesionCache = string.Join("; ", sessionCookies);

                return _sesionCache; // Devolverá algo como: "B1SESSION=xyz; ROUTEID=abc"
            }

            throw new Exception("No se pudo extraer la cookie B1SESSION.");
        }
    }
}