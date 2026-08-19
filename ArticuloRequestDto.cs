namespace Services.Identity.API.DTOs
{
    public class ArticuloRequestDto
    {
        public string ItemCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public int ItemsGroupCode { get; set; }
        public bool IsSalesItem { get; set; } = true;
        public bool IsInventoryItem { get; set; } = false;

        // Propiedades para identificar si trae Lista de Materiales (BOM)
        public bool TieneBOM { get; set; } = false;
        public List<BomLineDto> Componentes { get; set; } = new();
    }

    public class BomLineDto
    {
        public string ItemCode { get; set; } = string.Empty;
        public double Quantity { get; set; }
    }


}