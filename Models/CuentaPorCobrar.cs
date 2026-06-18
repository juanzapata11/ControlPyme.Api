using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ControlPyme.Api.Models
{
    [Table("CuentasPorCobrar")]
    public class CuentaPorCobrar
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public int ClienteId { get; set; }
        public int FacturaId { get; set; }
        public DateTime FechaEmision { get; set; }
        public DateTime FechaVencimiento { get; set; }
        public decimal ValorTotal { get; set; }
        public decimal SaldoActual { get; set; }
        public string Estado { get; set; } = "PENDIENTE"; // PENDIENTE, PAGADA, ANULADA
    }
}