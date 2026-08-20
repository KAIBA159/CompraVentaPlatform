using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace Services.Identity.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SystemController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public SystemController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet("info-conexion")]
        public IActionResult ObtenerInfoConexion()
        {
            try
            {
                // Lee el nombre exacto de la base de datos configurada para SAP B1
                string companyDb = _configuration["SapSettings:CompanyDB"] ?? "SBO_MAKITA_20260717";

                return Ok(new
                {
                    success = true,
                    databaseName = companyDb,
                    environment = "SAP Business One Service Layer"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }
    }
}