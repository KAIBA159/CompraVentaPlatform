namespace Services.Identity.API.DTOs
{
    public class ArticuloSimpleMigracionDto
    {
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public string ItemType { get; set; }
        public int ItemsGroupCode { get; set; }

        // Nuevos campos SUNAT / Negocio (Tipo String para mantener "01")
        public string U_EXX_TIPOEXIS { get; set; }
        public string U_EXX_TIPOUMED { get; set; }
        public string U_EXM_PERCOM { get; set; }
        public string U_EXM_ESTOBS { get; set; }
        public string U_MKA_TINCOS { get; set; }

        public List<PrecioItemDto> Precios { get; set; }
        //public List<ComponenteItemDto> Componentes { get; set; }
    }

    // <-- Esta clase debe estar aquí mismo para que Visual Studio la reconozca
    //public class PrecioItemDto
    //{
    //    public int PriceListId { get; set; } // Ej: 1 para Base, 3 para Distribuidor 1, etc.
    //    public double Price { get; set; }
    //}
}