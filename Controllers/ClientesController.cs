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

        [HttpPut("{id}/asignar-ruta")]
        public async Task<IActionResult> AsignarPosicionRuta(int id, [FromQuery] string estrategia, [FromQuery] int? clienteReferenciaId)
        {
            using var transaccion = await _context.Database.BeginTransactionAsync();
            try
            {
                var clienteNuevo = await _context.Clientes.FindAsync(id);
                if (clienteNuevo == null) return NotFound("Cliente no encontrado.");

                var todosLosClientes = await _context.Clientes
                    .Where(c => c.Id != id)
                    .OrderBy(c => c.OrdenRuta)
                    .ToListAsync();

                int nuevoOrden = 1;

                switch (estrategia.ToUpper().Trim())
                {
                    case "PRIMERO":
                        nuevoOrden = 1;
                        // Empujamos a todos los demás hacia adelante
                        foreach (var c in todosLosClientes) { c.OrdenRuta++; }
                        break;

                    case "ULTIMO":
                        int maxOrden = todosLosClientes.Any() ? todosLosClientes.Max(c => c.OrdenRuta) : 0;
                        nuevoOrden = maxOrden + 1;
                        break;

                    case "MISMA_ULTIMO":
                        nuevoOrden = todosLosClientes.Any() ? todosLosClientes.Max(c => c.OrdenRuta) : 1;
                        if (nuevoOrden == 0) nuevoOrden = 1;
                        break;

                    case "DESPUES_DE":
                        if (clienteReferenciaId == null) return BadRequest("Debe especificar un cliente de referencia.");

                        var clienteRef = todosLosClientes.FirstOrDefault(c => c.Id == clienteReferenciaId);
                        if (clienteRef == null) return BadRequest("El cliente de referencia no existe.");

                        nuevoOrden = clienteRef.OrdenRuta + 1;

                        // Empujamos solo a los que estaban después del cliente de referencia
                        foreach (var c in todosLosClientes.Where(c => c.OrdenRuta > clienteRef.OrdenRuta))
                        {
                            c.OrdenRuta++;
                        }
                        break;

                    default:
                        return BadRequest("Estrategia de ruteo no válida.");
                }

                // Asignamos el orden calculado al cliente nuevo
                clienteNuevo.OrdenRuta = nuevoOrden;

                await _context.SaveChangesAsync();
                await transaccion.CommitAsync();

                return Ok(new { mensaje = "Ruta organizada con éxito", ordenAsignado = nuevoOrden });
            }
            catch (Exception ex)
            {
                await transaccion.RollbackAsync();
                return StatusCode(500, $"Error al organizar ruta: {ex.Message}");
            }
        }

        //// GET: api/clientes
        //[HttpGet]
        //public async Task<ActionResult<IEnumerable<Cliente>>> GetClientes()
        //{
        //    return await _context.Clientes.ToListAsync();
        //}

        // GET: api/Clientes
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Cliente>>> GetClientes()
        {
            // 1. Traemos la lista de clientes tal como están en la base de datos (con su CupoDisponible real)
            var clientes = await _context.Clientes.ToListAsync();

            // 2. Traemos todas las cuentas por cobrar que tengan deudas activas
            var cxcPendientes = await _context.CuentasPorCobrar
                .Where(c => c.Estado == "PENDIENTE")
                .ToListAsync();

            // 3. Calculamos el SaldoPendiente para inyectarlo en la propiedad [NotMapped]
            foreach (var cliente in clientes)
            {
                // Sumamos el saldo actual de todas las facturas que deba este cliente
                cliente.SaldoPendiente = cxcPendientes
                    .Where(c => c.ClienteId == cliente.Id)
                    .Sum(c => c.SaldoActual);

                // NOTA: No tocamos cliente.CupoDisponible aquí porque ya viene con el valor real y actualizado desde tu base de datos SQL Server.
            }

            return clientes;
        }

        // POST: api/clientes
        [HttpPost]
        public async Task<ActionResult<Cliente>> PostCliente(Cliente cliente)
        {
            _context.Clientes.Add(cliente);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetClientes), new { id = cliente.Id }, cliente);
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
