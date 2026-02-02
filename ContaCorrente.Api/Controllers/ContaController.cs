using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using BancoDigitalAna.ContaCorrente.Api.Models;
using BancoDigitalAna.ContaCorrente.Api.Services;
using System.Security.Claims;

namespace BancoDigitalAna.ContaCorrente.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ContaController : ControllerBase
    {
        private readonly IContaService _contaService;

        public ContaController(IContaService contaService)
        {
            _contaService = contaService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest req)
        {
            try
            {
                var (numero, id) = await _contaService.RegisterAsync(req.Cpf, req.Senha, req.Nome);
                return Created(string.Empty, new { numero });
            }
            catch (ArgumentException ex) when (ex.ParamName == "INVALID_DOCUMENT")
            {
                return BadRequest(new { message = "CPF inválido", type = "INVALID_DOCUMENT" });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest req)
        {
            var token = await _contaService.LoginAsync(req.Login, req.Senha);
            if (token == null) return Unauthorized(new { message = "Usuário não autorizado", type = "USER_UNAUTHORIZED" });

            return Ok(new { token });
        }

        [Authorize(AuthenticationSchemes = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme)]
        [HttpPost("inactivate")]
        public async Task<IActionResult> Inactivate([FromBody] InactivateRequest req)
        {
	
            
			var accountId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (accountId == null) return Forbid();

            var success = await _contaService.InactivateAsync(accountId, req.senha);
            if (!success) return BadRequest(new { message = "Conta inválida", type = "INVALID_ACCOUNT" });
            
            return NoContent();
        }

        [Authorize]
        [HttpPost("movimentacao")]
        public async Task<IActionResult> Movimentacao([FromBody] MovimentacaoRequest req)
        {
            string accountId;
		
			     if (!string.IsNullOrEmpty(req.NumeroConta))
				 {
					 // Buscar id pela numero
					 var acc = await _contaService.GetAccountIdByNumeroAsync(req.NumeroConta);
					 if (acc == null) return BadRequest(new { message = "Conta inválida", type = "INVALID_ACCOUNT" });
					 accountId = acc;
				 }
				 else
				 {
					 accountId = User.FindFirst("sub")?.Value;
					 if (accountId == null) return Forbid();
				 }

				 try
				 {
					 var success = await _contaService.MovimentacaoAsync(accountId, req.Valor, req.Tipo, req.IdentificacaoRequisicao);
					 return NoContent();
				 }
				 catch (ArgumentException ex) when (ex.ParamName == "INVALID_ACCOUNT")
				 {
					 return BadRequest(new { message = "Conta inválida", type = "INVALID_ACCOUNT" });
				 }
				 catch (ArgumentException ex) when (ex.ParamName == "INACTIVE_ACCOUNT")
				 {
					 return BadRequest(new { message = "Conta inativa", type = "INACTIVE_ACCOUNT" });
				 }
				 catch (ArgumentException ex) when (ex.ParamName == "INVALID_VALUE")
				 {
					 return BadRequest(new { message = "Valor inválido", type = "INVALID_VALUE" });
				 }
				 catch (ArgumentException ex) when (ex.ParamName == "INVALID_TYPE")
				 {
					 return BadRequest(new { message = "Tipo inválido", type = "INVALID_TYPE" });
				 }
		}

        [Authorize]
        [HttpGet("saldo")]
        public async Task<IActionResult> Saldo()
        {
			var accountId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
			if (accountId == null) return Forbid();

            try
            {
                var (numero, nome, saldo) = await _contaService.GetSaldoAsync(accountId);
                return Ok(new { numero, nome, data = DateTime.UtcNow, saldo });
            }
            catch (ArgumentException ex) when (ex.ParamName == "INVALID_ACCOUNT")
            {
                return BadRequest(new { message = "Conta inválida", type = "INVALID_ACCOUNT" });
            }
            catch (ArgumentException ex) when (ex.ParamName == "INACTIVE_ACCOUNT")
            {
                return BadRequest(new { message = "Conta inativa", type = "INACTIVE_ACCOUNT" });
            }
        }
    }
}
