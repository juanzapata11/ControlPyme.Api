using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace ControlPyme.Api.Models
{
    public class Cliente
    {
        public int Id { get; set; }
        [JsonPropertyName("NombreCompleto")]
        public string NombreCompleto { get; set; } = string.Empty;
        public string? Telefono { get; set; }
        public string? TipoDocumento { get; set; }
        public string? NumeroDocumento { get; set; }
        public string? Celular { get; set; }
        public string? Oficio { get; set; }
        public string? Ciudad { get; set; }
        public string? DirDomicilio { get; set; }
        public string? BarrioDomicilio { get; set; }
        public string? DirCobro { get; set; }
        public string? BarrioCobro { get; set; }
        public string? NotasGenerales { get; set; }
        public int UsuarioId { get; set; }

        
        [JsonPropertyName("CupoDisponible")]
        public decimal CupoDisponible { get; set; }
        
        [NotMapped]
        [JsonPropertyName("SaldoPendiente")]
        public decimal SaldoPendiente { get; set; }

        // Propiedad calculada que ayuda a la UI a saber si muestra alertas en rojo
        [JsonIgnore] // Evita que se intente enviar o serializar este campo hacia la API
        public bool TieneSaldoPendiente => SaldoPendiente > 0;
    }
}
