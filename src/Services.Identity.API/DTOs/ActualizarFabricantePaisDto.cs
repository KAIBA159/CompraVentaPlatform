namespace Services.Identity.API.DTOs
{
    public class ActualizarFabricantePaisDto
    {

        public string ItemCode { get; set; } = string.Empty;
        public int Manufacturer { get; set; } // FirmCode del Excel (Ej: 2)
        public string CountryOfOrigin { get; set; } = string.Empty; // ISOriCntry del Excel (Ej: "BR")

    }
}
