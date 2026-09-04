namespace Services.Identity.API.DTOs
{
    public class ActualizarPrecioListaDto
    {

        public string ItemCode { get; set; } = string.Empty;
        public int PriceListId { get; set; }
        public decimal Price { get; set; }
        public string Currency { get; set; } = "USD"; // Nueva propiedad

    }
}
