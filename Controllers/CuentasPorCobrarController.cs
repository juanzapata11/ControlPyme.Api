using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ControlPyme.Api.Data;
using ControlPyme.Api.Models;

namespace ControlPyme.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")] // Esto mapea a: api/CuentasPorCobrar
    public class CuentasPorCobrarController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CuentasPorCobrarController(AppDbContext context)
        {
            _context = context;
        }
        
        [HttpGet("cliente/{clienteId}")]
        public async Task<ActionResult<IEnumerable<CuentaPorCobrar>>> GetCuentasPorCliente(int clienteId)
        {
            // Buscamos solo las facturas a crédito pendientes por pagar del cliente solicitado
            var cuentas = await _context.CuentasPorCobrar
                .Where(c => c.ClienteId == clienteId && c.Estado == "PENDIENTE")
                .ToListAsync();

            // Si todo va bien, devolvemos la lista (así esté vacía devuelve un 200 OK con [] )
            return Ok(cuentas);
        }
    }
}