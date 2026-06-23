namespace ControlPyme.Api.Models
{
    public class Factura
    {
        public int Id { get; set; }
        public int ClienteId { get; set; }
        public DateTime FechaVenta { get; set; }
        public string TipoPago { get; set; } // "Contado" o "Crédito"
        public decimal Subtotal { get; set; }
        public decimal TotalPagar { get; set; }
        public int UsuarioId { get; set; }

        // Propiedad de navegación: Una factura contiene múltiples filas de detalle
        public List<DetalleFactura> Detalles { get; set; } = new List<DetalleFactura>();
        public string? EstrategiaRuta { get; set; }
        public int? ClienteReferenciaId { get; set; }
    }
}
