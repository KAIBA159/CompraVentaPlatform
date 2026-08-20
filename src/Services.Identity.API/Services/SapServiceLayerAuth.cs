using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace Services.Identity.API.Services
{
    public class SapServiceLayerAuth
    {
        private readonly string _baseUrl = "https://192.168.1.17:50000/b1s/v1/";
        private readonly IConfiguration _configuration;

        public SapServiceLayerAuth(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<string> ObtenerCookieSesionAsync()
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };

            using var client = new HttpClient(handler) { BaseAddress = new Uri(_baseUrl) };

            // Leemos dinámicamente desde appsettings.json con respaldo a tu BD actual
            string miBaseDatos = _configuration["SapSettings:CompanyDB"] ?? "SBO_MAKITA_20260717";
            string miUsuario = _configuration["SapSettings:UserName"] ?? "manager";
            string miClave = _configuration["SapSettings:Password"] ?? "m1r1";

            string loginJson = $@"{{
                ""CompanyDB"": ""{miBaseDatos}"",
                ""UserName"": ""{miUsuario}"",
                ""Password"": ""{miClave}""
            }}";

            var content = new StringContent(loginJson, Encoding.UTF8, "application/json");
            var response = await client.PostAsync("Login", content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"Fallo de autenticación en Service Layer SAP B1: {errorContent}");
            }

            if (response.Headers.TryGetValues("Set-Cookie", out var cookies))
            {
                foreach (var cookie in cookies)
                {
                    if (cookie.Contains("B1SESSION"))
                    {
                        var parts = cookie.Split(';');
                        return parts[0];
                    }
                }
            }

            throw new Exception("No se pudo extraer la cookie B1SESSION tras el login exitoso.");
        }
    }
}