namespace Services.Identity.API.DTOs
{
    public class ArticuloMigracionDto
    {
        // Datos básicos obligatorios
        public string ItemCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty; // <-- Corregido el punto por punto y coma
        public int ItemsGroupCode { get; set; }

        // Indicador si es un Combo (Lista de Materiales)
        public bool EsCombo { get; set; } = false;

        // Múltiples Listas de Precios (Mapeadas por ID de lista en SAP)
        public List<PrecioItemDto> Precios { get; set; } = new();

        // Componentes (Solo si EsCombo = true)
        public List<ComponenteComboDto> Componentes { get; set; } = new();
    }

    public class PrecioItemDto
    {
        public int PriceListId { get; set; } // Ejemplo: 1 para Base, 3 para Distribuidor 1, etc.
        public double Price { get; set; }
    }

    public class ComponenteComboDto
    {
        public string ItemCode { get; set; } = string.Empty; // Ej: D-2431
        public double Quantity { get; set; }                  // Ej: 300
    }
}