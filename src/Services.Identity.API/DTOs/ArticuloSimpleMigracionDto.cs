namespace Services.Identity.API.DTOs
{
    // 1. CLASE BASE
    public class ArticuloSimpleMigracionDto
    {
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public string ItemType { get; set; }
        public int ItemsGroupCode { get; set; }

        // Campos de Usuario (UDF - SUNAT y adicionales)
        public string U_EXX_TIPOEXIS { get; set; }
        public string U_EXX_TIPOUMED { get; set; }
        public string U_EXM_PERCOM { get; set; }
        public string U_EXM_ESTOBS { get; set; }
        public string U_MKA_TINCOS { get; set; }

        public List<PrecioItemDto> Precios { get; set; } = new();
    }

    // 2. CLASE COMBO
    public class ArticuloComboMigracionDto : ArticuloSimpleMigracionDto
    {
        public string InvntItem { get; set; }
        public string SellItem { get; set; }
        public string Prchselitem { get; set; }
        public string TreeType { get; set; }
        public List<ComponenteBOMDto> Componentes { get; set; } = new();
    }

    // 3. CLASES DE APOYO 
    public class PrecioItemDto
    {
        public int PriceListId { get; set; }
        public decimal Price { get; set; }
    }

    public class ComponenteBOMDto
    {
        public string ItemCode { get; set; }
        public decimal Quantity { get; set; }
    }
}