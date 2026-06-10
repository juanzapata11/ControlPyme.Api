using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ControlPyme.Api.Data;
using ControlPyme.Api.Models;
//
namespace ControlPyme.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ClientesController : ControllerBase
    {

        private readonly AppDbContext _context;

        public ClientesController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/clientes
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Cliente>>> GetClientes()
        {
            return await _context.Clientes.ToListAsync();
        }

        // GET: api/clientes/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Cliente>> GetCliente(int id)
        {
            var cliente = await _context.Clientes.FindAsync(id);

            if (cliente == null)
                return NotFound();

            return cliente;
        }

        // POST: api/clientes
        [HttpPost]
        public async Task<ActionResult<Cliente>> PostCliente(Cliente cliente)
        {
            _context.Clientes.Add(cliente);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetCliente), new { id = cliente.Id }, cliente);
        }

        // PUT: api/clientes/5
        [HttpPut("{id}")]
        public async Task<IActionResult> ActualizarCliente(int id, [FromBody] Cliente clienteEditado)
        {
            if (id != clienteEditado.Id)
            {
                return BadRequest("El ID del cliente no coincide con la petición.");
            }

            // 1. Buscamos el registro real actual en la base de datos
            var clienteDb = await _context.Clientes.FindAsync(id);
            if (clienteDb == null)
            {
                return NotFound($"El cliente con ID {id} no existe.");
            }

            // 2. Actualizamos TODOS los campos con lo que viene de MAUI
            clienteDb.NombreCompleto = clienteEditado.NombreCompleto;
            clienteDb.NumeroDocumento = clienteEditado.NumeroDocumento;
            clienteDb.TipoDocumento = clienteEditado.TipoDocumento;
            clienteDb.Oficio = clienteEditado.Oficio;
            clienteDb.Celular = clienteEditado.Celular;
            clienteDb.Telefono = clienteEditado.Telefono;
            clienteDb.Ciudad = clienteEditado.Ciudad;
            clienteDb.DirDomicilio = clienteEditado.DirDomicilio;
            clienteDb.BarrioDomicilio = clienteEditado.BarrioDomicilio;
            clienteDb.DirCobro = clienteEditado.DirCobro;
            clienteDb.BarrioCobro = clienteEditado.BarrioCobro;
            clienteDb.NotasGenerales = clienteEditado.NotasGenerales;
            clienteDb.CupoDisponible = clienteEditado.CupoDisponible;

            try
            {
                await _context.SaveChangesAsync();
                return NoContent(); // Devuelve el 204 con éxito total
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Clientes.Any(e => e.Id == id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
        }

        // DELETE: api/clientes/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCliente(int id)
        {
            var cliente = await _context.Clientes.FindAsync(id);

            if (cliente == null)
                return NotFound();

            _context.Clientes.Remove(cliente);
            await _context.SaveChangesAsync();

            return NoContent();
        }

    }
}
