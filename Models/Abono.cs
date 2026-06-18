using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ControlPyme.Api.Models
{
    [Table("Abonos")]
    public class Abono
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public int CuentaPorCobrarId { get; set; }
        public DateTime FechaAbono { get; set; }
        public decimal MontoAbonado { get; set; }
        public string MetodoPago { get; set; } = "EFECTIVO";
        public int UsuarioId { get; set; }
    }
}