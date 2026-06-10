using ControlPyme.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ControlPyme.Api.Models; // Ajusta al namespace real de tus modelos compartidos
using System;
using System.Threading.Tasks;

namespace ControlPyme.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FacturasController : ControllerBase
    {
        //private readonly ApplicationDbContext _context; // Tu DbContext de Entity Framework
        private readonly AppDbContext _context;

        public FacturasController(AppDbContext context)
        {
            _context = context;
        }

        // POST: api/Facturas
        [HttpPost]
        public async Task<IActionResult> CrearFactura([FromBody] Factura nuevaFactura)
        {
            if (nuevaFactura == null || nuevaFactura.Detalles == null || !nuevaFactura.Detalles.Any())
            {
                return BadRequest("La factura no contiene productos o los datos son inválidos.");
            }

            using var transaccion = await _context.Database.BeginTransactionAsync();

            try
            {
                // 1. Forzar la fecha y hora exacta del servidor
                nuevaFactura.FechaVenta = DateTime.Now;

                // 2. REGLA DE ORO: Validamos y descontamos stock ANTES de guardar la factura.
                // Así no modificamos propiedades de 'detalle' que puedan corromper el tracking de EF.
                foreach (var detalle in nuevaFactura.Detalles)
                {
                    // Buscamos el producto en la base de datos
                    var producto = await _context.Productos.FindAsync(detalle.ProductoId);
                    if (producto == null)
                    {
                        throw new Exception($"El producto con ID {detalle.ProductoId} no existe en el inventario.");
                    }

                    // Validamos existencias en la peluquería
                    if (producto.StockActual < detalle.Cantidad)
                    {
                        throw new Exception($"Stock insuficiente para {producto.Nombre}. Disponible: {producto.StockActual}, Solicitado: {detalle.Cantidad}");
                    }

                    // Descontamos las unidades del inventario
                    producto.StockActual -= detalle.Cantidad;
                    _context.Entry(producto).State = EntityState.Modified;
                }

                // 3. Si la venta es a Crédito, afectamos la cartera del cliente inmediatamente
                if (nuevaFactura.TipoPago == "CREDITO")
                {
                    var cliente = await _context.Clientes.FindAsync(nuevaFactura.ClienteId);
                    if (cliente != null)
                    {
                        cliente.SaldoPendiente += nuevaFactura.TotalPagar;
                        _context.Entry(cliente).State = EntityState.Modified;
                    }
                }

                // 4. GUARDADO MAESTRO: Agregamos la factura completa al contexto.
                // EF se encargará de insertar la Factura, generar su Id, asignárselo a los detalles
                // e insertar cada DetalleFactura con su respectivo ID autonumérico en un solo paso físico.
                _context.Facturas.Add(nuevaFactura);

                // Guardamos todo de forma masiva y segura
                await _context.SaveChangesAsync();

                // Confirmamos la transacción en SQL Server
                await transaccion.CommitAsync();

                return CreatedAtAction(nameof(CrearFactura), new { id = nuevaFactura.Id }, nuevaFactura);
            }
            catch (Exception ex)
            {
                await transaccion.RollbackAsync();

                string errorDetallado = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return StatusCode(500, $"Error interno al procesar la venta: {errorDetallado}");
            }
        }
    }
}