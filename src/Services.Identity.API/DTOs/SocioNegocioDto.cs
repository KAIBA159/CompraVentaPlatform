namespace Services.Identity.API.DTOs
{
    public class SocioNegocioDto
    {

        // El Front-end enviará "C" o "P"
        public string TipoSocio { get; set; } = "C";

        // RUC o DNI
        public string Documento { get; set; } = string.Empty;

        public string RazonSocial { get; set; } = string.Empty;
        public string Direccion { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public string CorreoElectronico { get; set; } = string.Empty;

        // Propiedad calculada en el Backend para cumplir tu regla
        public string CodigoGenerado => $"{TipoSocio}-{Documento}";

    }
}
