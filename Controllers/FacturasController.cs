using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ControlPyme.Api.Data;
using ControlPyme.Api.Models;

namespace ControlPyme.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FacturasController : ControllerBase
    {
        private readonly AppDbContext _context;

        public FacturasController(AppDbContext context)
        {
            _context = context;
        }

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

                // 2. Validamos y descontamos stock en el inventario
                foreach (var detalle in nuevaFactura.Detalles)
                {
                    var producto = await _context.Productos.FindAsync(detalle.ProductoId);
                    if (producto == null)
                    {
                        throw new Exception($"El producto con ID {detalle.ProductoId} no existe en el inventario.");
                    }

                    if (producto.StockActual < detalle.Cantidad)
                    {
                        throw new Exception($"Stock insuficiente para {producto.Nombre}. Disponible: {producto.StockActual}, Solicitado: {detalle.Cantidad}");
                    }

                    producto.StockActual -= detalle.Cantidad;
                    _context.Entry(producto).State = EntityState.Modified;
                }

                // 3. GUARDADO INTERMEDIO DE LA FACTURA
                // Necesitamos agregar la factura primero para que SQL Server genere su ID autonumérico
                _context.Facturas.Add(nuevaFactura);
                await _context.SaveChangesAsync();

                // 🔥 BLINDAJE: Limpiamos el texto de espacios y mayúsculas para evitar fallas de comparación
                string metodoPagoLimpio = nuevaFactura.TipoPago?.ToUpper().Trim() ?? "CONTADO";

                // 4. Si la venta es a Crédito, afectamos la cartera del cliente, CREAMOS LA CXC y ORGANIZAMOS RUTA
                if (metodoPagoLimpio == "CREDITO")
                {
                    var cliente = await _context.Clientes.FindAsync(nuevaFactura.ClienteId);
                    if (cliente == null)
                    {
                        throw new Exception("El cliente especificado para la venta a crédito no existe.");
                    }

                    // Validamos si el cliente tiene cupo disponible para esta compra
                    if (nuevaFactura.TotalPagar > cliente.CupoDisponible)
                    {
                        throw new Exception($"Crédito rechazado. El total de la venta (${nuevaFactura.TotalPagar:#,##0}) supera el cupo disponible del cliente (${cliente.CupoDisponible:#,##0}).");
                    }

                    // Afectamos los saldos globales del cliente
                    cliente.SaldoPendiente += nuevaFactura.TotalPagar;
                    cliente.CupoDisponible -= nuevaFactura.TotalPagar;
                    _context.Entry(cliente).State = EntityState.Modified;

                    // AQUÍ NACE LA CUENTA POR COBRAR ASOCIADA A ESTA FACTURA:
                    var nuevaCxc = new CuentaPorCobrar
                    {
                        ClienteId = nuevaFactura.ClienteId,
                        FacturaId = nuevaFactura.Id, // Usamos el ID recién generado por SQL Server
                        FechaEmision = DateTime.Now,
                        FechaVencimiento = DateTime.Now.AddDays(30), // Plazo estándar de 30 días para las peluquerías
                        ValorTotal = nuevaFactura.TotalPagar,
                        SaldoActual = nuevaFactura.TotalPagar, // Inicia debiendo el 100%
                        Estado = "PENDIENTE"
                    };

                    _context.CuentasPorCobrar.Add(nuevaCxc);

                    // =========================================================================
                    // 🚀 NUEVA LÓGICA DE ENRUTAMIENTO GEOGRÁFICO AUTOMÁTICO
                    // =========================================================================
                    string estrategia = nuevaFactura.EstrategiaRuta ?? "ULTIMO";
                    int? referenciaId = nuevaFactura.ClienteReferenciaId;

                    // Obtenemos todos los DEMÁS clientes ordenados por su posición de ruta actual
                    var todosLosClientes = await _context.Clientes
                        .Where(c => c.Id != nuevaFactura.ClienteId)
                        .OrderBy(c => c.OrdenRuta)
                        .ToListAsync();

                    int nuevoOrden = 1;

                    if (estrategia == "PRIMERO")
                    {
                        nuevoOrden = 1;
                        // Desplazamos a todos un puesto hacia adelante para liberar el primer lugar
                        foreach (var c in todosLosClientes) { c.OrdenRuta++; }
                    }
                    else if (estrategia == "DESPUES_DE" && referenciaId.HasValue)
                    {
                        var cRef = todosLosClientes.FirstOrDefault(c => c.Id == referenciaId.Value);
                        if (cRef != null)
                        {
                            nuevoOrden = cRef.OrdenRuta + 1;
                            // Desplazamos solo a los que queden rezagados detrás del cliente de referencia
                            foreach (var c in todosLosClientes.Where(x => x.OrdenRuta > cRef.OrdenRuta))
                            {
                                c.OrdenRuta++;
                            }
                        }
                    }
                    else // Caso: "ULTIMO" o "MISMA_ULTIMO"
                    {
                        int max = todosLosClientes.Any() ? todosLosClientes.Max(x => x.OrdenRuta) : 0;
                        nuevoOrden = estrategia == "MISMA_ULTIMO" ? (max == 0 ? 1 : max) : max + 1;
                    }

                    // Asignamos el puesto geográfico calculado al cliente dueño de esta factura
                    cliente.OrdenRuta = nuevoOrden;
                    _context.Entry(cliente).State = EntityState.Modified;
                    // =========================================================================
                }

                // 5. Guardamos de forma definitiva (Modificaciones de Cliente, Stock, Nueva CXC y Orden de Ruta)
                await _context.SaveChangesAsync();

                // Confirmamos la transacción limpia en SQL Server de manera segura
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