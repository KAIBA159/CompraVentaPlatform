using Microsoft.Extensions.Options;
using Services.Identity.API.DTOs;
using System.Text;
using System.Text.Json;

namespace Services.Identity.API.Services
{
    public class SapSocioNegocioService
    {

        private readonly SapSettings _sapSettings;
        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = null // Obligatorio para respetar mayúsculas de SAP
        };

        public SapSocioNegocioService(IOptions<SapSettings> sapSettings)
        {
            _sapSettings = sapSettings.Value;
        }

        // ==============================================================================
        // 1. CREAR SOCIO DE NEGOCIO (POST)
        // ==============================================================================
        public async Task<(bool Exito, string Mensaje)> CrearSocioNegocioAsync(
            SocioNegocioDto dto, string sessionCookie, HttpClient client)
        {
            try
            {
                EnsureCookie(client, sessionCookie);

                // REGLAS DE NEGOCIO APLICADAS:
                // 1. Concatenación de Tipo + Documento (Ej: C20123456789 o P72123456)
                string cardCode = $"{dto.TipoSocio.ToUpper()}{dto.Documento}";
                // 2. Diferenciación de Tipo
                string cardType = dto.TipoSocio.ToUpper() == "P" ? "cSupplier" : "cCustomer";

                var bpPayload = new
                {
                    CardCode = cardCode,
                    CardName = dto.RazonSocial,
                    CardType = cardType,
                    FederalTaxID = dto.Documento, // RUC/DNI en SAP
                    Phone1 = dto.Telefono,
                    EmailAddress = dto.CorreoElectronico,
                    Address = dto.Direccion,
                    Valid = "tYES" // Se crea activo por defecto
                };

                var response = await client.PostAsJsonAsync("BusinessPartners", bpPayload, _jsonOptions);

                if (!response.IsSuccessStatusCode)
                {
                    return (false, $"Error al crear Socio de Negocio: {await response.Content.ReadAsStringAsync()}");
                }

                return (true, $"Socio de negocio {cardCode} registrado correctamente en SAP B1.");
            }
            catch (Exception ex)
            {
                return (false, $"Excepción interna: {ex.Message}");
            }
        }

        // ==============================================================================
        // 2. ACTUALIZAR SOCIO DE NEGOCIO (PATCH)
        // ==============================================================================
        public async Task<(bool Exito, string Mensaje)> ActualizarSocioNegocioAsync(
            SocioNegocioDto dto, string sessionCookie, HttpClient client)
        {
            try
            {
                EnsureCookie(client, sessionCookie);

                string cardCode = $"{dto.TipoSocio.ToUpper()}{dto.Documento}";
                var patchPayload = new Dictionary<string, object>();

                if (!string.IsNullOrWhiteSpace(dto.RazonSocial)) patchPayload["CardName"] = dto.RazonSocial;
                if (!string.IsNullOrWhiteSpace(dto.Telefono)) patchPayload["Phone1"] = dto.Telefono;
                if (!string.IsNullOrWhiteSpace(dto.CorreoElectronico)) patchPayload["EmailAddress"] = dto.CorreoElectronico;
                if (!string.IsNullOrWhiteSpace(dto.Direccion)) patchPayload["Address"] = dto.Direccion;

                var content = new StringContent(JsonSerializer.Serialize(patchPayload, _jsonOptions), Encoding.UTF8, "application/json");
                var response = await client.PatchAsync($"BusinessPartners('{cardCode}')", content);

                if (!response.IsSuccessStatusCode)
                    return (false, $"Error al actualizar Socio de Negocio: {await response.Content.ReadAsStringAsync()}");

                return (true, $"Socio de negocio {cardCode} actualizado correctamente.");
            }
            catch (Exception ex)
            {
                return (false, $"Excepción interna: {ex.Message}");
            }
        }

        // ==============================================================================
        // 3. ANULAR SOCIO DE NEGOCIO - SOFT DELETE (PATCH)
        // ==============================================================================
        public async Task<(bool Exito, string Mensaje)> AnularSocioNegocioAsync(
            string tipoSocio, string documento, string sessionCookie, HttpClient client)
        {
            try
            {
                EnsureCookie(client, sessionCookie);
                string cardCode = $"{tipoSocio.ToUpper()}{documento}";

                // REGLA DE NEGOCIO APLICADA: Soft Delete
                // Congelamos e invalidamos el socio sin borrarlo de la base de datos
                var patchPayload = new
                {
                    Valid = "tNO",
                    Frozen = "tYES"
                };

                var content = new StringContent(JsonSerializer.Serialize(patchPayload, _jsonOptions), Encoding.UTF8, "application/json");
                var response = await client.PatchAsync($"BusinessPartners('{cardCode}')", content);

                if (!response.IsSuccessStatusCode)
                    return (false, $"Error al anular Socio de Negocio: {await response.Content.ReadAsStringAsync()}");

                return (true, $"Socio de negocio {cardCode} ha sido anulado (Soft Delete) exitosamente.");
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
