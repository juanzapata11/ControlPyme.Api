using ControlPyme.Shared.Models;
using Dapper;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Data.SqlClient;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BCrypt.Net;

namespace ControlPyme.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly string _connectionString;

    public AuthController(IConfiguration configuration)
    {
        _configuration = configuration;
        // Leemos la cadena de conexión a tu base de datos (configurada en appsettings.json)
        _connectionString = _configuration.GetConnectionString("DefaultConnection")!;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] ControlPyme.Shared.Models.LoginRequest request)
    {
        if (string.IsNullOrEmpty(request.Usuario) || string.IsNullOrEmpty(request.Password))
        {
            return BadRequest(new LoginResponse { Exito = false, Mensaje = "Campos obligatorios." });
        }

        using var conexion = new SqlConnection(_connectionString);

        // 2. ¡AQUÍ ESTÁ TU MODIFICACIÓN DE BASE DE DATOS!
        // Usamos parámetros (@Username) por seguridad estricta contra SQL Injection
        string sql = "SELECT * FROM Usuarios WHERE Username = @Username";

        // Ejecutamos la consulta usando Dapper mapeándolo a una clase interna o dinámica
        var usuarioDb = await conexion.QueryFirstOrDefaultAsync<dynamic>(sql, new { Username = request.Usuario });

        // 3. Si el usuario no existe en la base de datos
        if (usuarioDb == null)
        {
            return Unauthorized(new LoginResponse { Exito = false, Mensaje = "Usuario o contraseña incorrectos." });
        }

        // 4. Verificar si la contraseña coincide con el Hash seguro de la base de datos
        bool passwordValida = BCrypt.Net.BCrypt.Verify(request.Password, usuarioDb.PasswordHash);

        if (!passwordValida)
        {
            return Unauthorized(new LoginResponse { Exito = false, Mensaje = "Usuario o contraseña incorrectos." });
        }

        // 5. Si todo está OK, generamos el Token JWT firmado para MAUI
        string tokenGenerado = GenerarJwtToken(usuarioDb);

        return Ok(new LoginResponse
        {
            Exito = true,
            Mensaje = "¡Bienvenido!",
            Token = tokenGenerado,
            NombreUsuario = usuarioDb.NombreCompleto
        });
    }

    // Método auxiliar para armar el token JWT
    private string GenerarJwtToken(dynamic usuario)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new Claim(ClaimTypes.Name, usuario.Username),
            new Claim(ClaimTypes.GivenName, usuario.NombreCompleto)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7), // El token dura 7 días en el celular
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}