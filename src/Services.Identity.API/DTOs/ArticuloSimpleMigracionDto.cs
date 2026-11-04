using System.Text.Json.Serialization;

namespace Services.Identity.API.DTOs
{
    // 1. CLASE BASE
    public class ArticuloSimpleMigracionDto
    {
        public string ItemCode { get; set; } = string.Empty;
        public string? ItemName { get; set; }
        public string? ItemType { get; set; } = "I";
        public int ItemsGroupCode { get; set; }

        // Campos de Usuario (UDF) opcionales en Excel
        public string? U_EXX_TIPOEXIS { get; set; }
        public string? U_EXX_TIPOUMED { get; set; }
        public string? U_EXM_PERCOM { get; set; }
        public string? U_EXM_ESTOBS { get; set; }
        public string? U_MKA_TINCOS { get; set; }

        // Vigencia
        public string? Valid { get; set; } = "tYES";
        public string? ValidTo { get; set; }
        // ---------------------------------


        //public string WTLiable { get; set; }
        //public string VATLiable { get; set; }
        //public string IndirctTax { get; set; }



        public List<PrecioItemDto> Precios { get; set; } = new();
    }

    //// 2. CLASE COMBO
    //public class ArticuloComboMigracionDto : ArticuloSimpleMigracionDto
    //{
    //    public string InvntItem { get; set; }
    //    public string SellItem { get; set; }
    //    public string Prchselitem { get; set; }
    //    public string TreeType { get; set; }
    //    public List<ComponenteBOMDto> Componentes { get; set; } = new();
    //}

    // 2. CLASE COMBO
    public class ArticuloComboMigracionDto : ArticuloSimpleMigracionDto
    {
        public string? TreeType { get; set; } = "iSales";
        public List<ComponenteBOMDto> Componentes { get; set; } = new();
    }


    //hola itamar

    // 3. CLASES DE APOYO
    public class PrecioItemDto
    {
        public int PriceListId { get; set; }
        public decimal Price { get; set; }
    }

    public class ComponenteBOMDto
    {
        public string ItemCode { get; set; } = string.Empty;
        public decimal Quantity { get; set; } = 1;
        public string? Warehouse { get; set; } = "ALM01";
        public int PriceList { get; set; } = 1;
    }


}