namespace Services.Identity.API.DTOs
{
    public class ActualizarFabricantePaisDto
    {

        public string ItemCode { get; set; }

        // El signo de interrogación (?) lo hace anulable, solucionando el CS1061
        public int? Manufacturer { get; set; }

        public string? CountryOfOrigin { get; set; }

        // NUEVO CAMPO: Agregado como anulable (?)
        public string? U_MKA_CIF { get; set; }

    }
}
