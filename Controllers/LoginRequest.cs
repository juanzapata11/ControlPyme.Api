namespace ControlPyme.Shared.Models;

// Lo que la App le envía a la API
public class LoginRequest
{
    public string Usuario { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}


public class LoginResponse
{
    public bool Exito { get; set; }
    public string Mensaje { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty; // El JWT string
    public string NombreUsuario { get; set; } = string.Empty;
}