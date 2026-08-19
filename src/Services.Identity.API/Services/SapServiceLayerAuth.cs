using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Services.Identity.API.Services
{
    public class SapServiceLayerAuth
    {
        private readonly string _baseUrl = "https://192.168.1.17:50000/b1s/v1/";

        public async Task<string> ObtenerCookieSesionAsync()
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };

            using var client = new HttpClient(handler) { BaseAddress = new Uri(_baseUrl) };

            // Declaramos las credenciales exactas igual que en tu WinForms
            string miBaseDatos = "SBO_MAKITA_20260717";
            string miUsuario = "manager";
            string miClave = "m1r1";

            // Armamos el JSON literal idéntico al que le gusta a la Service Layer
            string loginJson = $@"{{
                ""CompanyDB"": ""{miBaseDatos}"",
                ""UserName"": ""{miUsuario}"",
                ""Password"": ""{miClave}""
            }}";

            var content = new StringContent(loginJson, Encoding.UTF8, "application/json");

            // Ejecutamos el POST al endpoint de Login
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