using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ControlPyme.Api.Data;
using ControlPyme.Api.Models;

namespace ControlPyme.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AbonosController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AbonosController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> RegistrarAbono([FromBody] Abono nuevoAbono)
        {
            if (nuevoAbono == null || nuevoAbono.MontoAbonado <= 0)
            {
                return BadRequest("El monto del abono debe ser mayor a cero.");
            }

            using var transaccion = await _context.Database.BeginTransactionAsync();

            try
            {
                nuevoAbono.FechaAbono = DateTime.Now;

                // 1. Buscar la Cuenta por Cobrar afectada
                var cxc = await _context.CuentasPorCobrar.FindAsync(nuevoAbono.CuentaPorCobrarId);
                if (cxc == null)
                {
                    return NotFound("La cuenta por cobrar especificada no existe.");
                }

                // Validar que no abone más de lo que debe en esa factura
                if (nuevoAbono.MontoAbonado > cxc.SaldoActual)
                {
                    return BadRequest($"El abono (${nuevoAbono.MontoAbonado:#,##0}) supera el saldo actual de la deuda (${cxc.SaldoActual:#,##0}).");
                }

                // 2. Buscar al Cliente para actualizar su saldo global y liberar cupo
                var cliente = await _context.Clientes.FindAsync(cxc.ClienteId);
                if (cliente == null)
                {
                    return NotFound("No se encontró el cliente asociado a esta cuenta por cobrar.");
                }

                // 3. APLICAR OPERACIONES MATEMÁTICAS
                cxc.SaldoActual -= nuevoAbono.MontoAbonado;
                cliente.SaldoPendiente -= nuevoAbono.MontoAbonado;
                cliente.CupoDisponible += nuevoAbono.MontoAbonado;

                // Si la factura ya se pagó por completo, cambiamos su estado
                if (cxc.SaldoActual == 0)
                {
                    cxc.Estado = "PAGADA";
                }

                // Marcar entidades como modificadas
                _context.Entry(cxc).State = EntityState.Modified;
                _context.Entry(cliente).State = EntityState.Modified;

                // 4. REGISTRAR EL ABONO FÍSICO
                _context.Abonos.Add(nuevoAbono);

                // Guardar todo en SQL Server de forma segura
                await _context.SaveChangesAsync();
                await transaccion.CommitAsync();

                return Ok(new
                {
                    Mensaje = "Abono aplicado con éxito",
                    SaldoRestanteFactura = cxc.SaldoActual,
                    SaldoGlobalCliente = cliente.SaldoPendiente
                });
            }
            catch (Exception ex)
            {
                await transaccion.RollbackAsync();
                string errorDetallado = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return StatusCode(500, $"Error interno al registrar el abono: {errorDetallado}");
            }
        }
    }
}