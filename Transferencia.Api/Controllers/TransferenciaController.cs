using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using BancoDigitalAna.Transferencia.Api.Services;
using BancoDigitalAna.Transferencia.Api.Models;

namespace BancoDigitalAna.Transferencia.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TransferenciaController : ControllerBase
    {
        private readonly ITransferenciaService _transferenciaService;

        public TransferenciaController(ITransferenciaService transferenciaService)
        {
            _transferenciaService = transferenciaService;
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Transfer([FromBody] TransferRequest req)
        {
            try
            {
                var accountNumber = User.FindFirst("accountNumber")?.Value;
                if (accountNumber == null) return Forbid();

                var authHeader = Request.Headers["Authorization"].ToString();

                await _transferenciaService.TransferAsync(req, accountNumber, authHeader);

                return NoContent();
            }
            catch (ArgumentException ex) when (ex.ParamName == "INVALID_VALUE")
            {
                return BadRequest(new { message = "Valor inválido", type = "INVALID_VALUE" });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (ArgumentException ex) when (ex.ParamName == "INVALID_ACCOUNT")
            {
                return BadRequest(new { message = ex.Message, type = "INVALID_ACCOUNT" });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Erro interno", type = "INTERNAL_ERROR" });
            }
        }
    }

}
