namespace ControlPyme.Api.Models
{
    public class Producto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public decimal PrecioVenta { get; set; }
        public int StockActual { get; set; }
        public int StockMinimo { get; set; }
        public bool EsCreditoPermitido { get; set; } = true;
        // Código de barras o SKU opcional para inventario
        public string CodigoBarras { get; set; } = string.Empty;
    }
}
