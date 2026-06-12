using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MxmChallenge.Data;

namespace MxmChallenge.Controllers
{
    [ApiController]
    [Route("api/health")]
    public class HealthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public HealthController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Retorna o status da API e da conexao com o SQL Server.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var databaseAvailable = await _context.Database.CanConnectAsync();

            return databaseAvailable
                ? Ok(new { status = "healthy", database = "reachable" })
                : StatusCode(StatusCodes.Status503ServiceUnavailable, new { status = "degraded", database = "unreachable" });
        }
    }
}
