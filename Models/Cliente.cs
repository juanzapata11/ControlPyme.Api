namespace ControlPyme.Api.Models
{
    public class Cliente//
    {
        public int Id { get; set; }
        public string? NombreCompleto { get; set; }
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
    }
}
