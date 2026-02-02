using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Data;
using Dapper;

namespace BancoDigitalAna.Transferencia.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TransferenciaController : ControllerBase
    {
        private readonly IHttpClientFactory _http;
        private readonly IDbConnection _db;

        private readonly Confluent.Kafka.IProducer<Confluent.Kafka.Null, string> _producer;

        public TransferenciaController(IHttpClientFactory http, IDbConnection db, Confluent.Kafka.IProducer<Confluent.Kafka.Null, string> producer)
        {
            _http = http;
            _db = db;
            _producer = producer;
        }

        [Authorize]
        [HttpPost]
        public IActionResult Transfer([FromBody] TransferRequest req)
        {
            // Basic validations
            if (req.Valor <= 0) return BadRequest(new { message = "Valor inválido", type = "INVALID_VALUE" });

            // Call ContaCorrente API to debit
            var client = _http.CreateClient();
            // In a real environment, use service discovery or config. For now expect CONTA_BASE_URL env var
            var baseUrl = Environment.GetEnvironmentVariable("CONTA_BASE_URL") ?? "http://contacorrente:5000";

            var debit = new { IdentificacaoRequisicao = req.IdentificacaoRequisicao, Valor = req.Valor, Tipo = "D" };
            var response = client.PostAsJsonAsync($"{baseUrl}/api/conta/movimentacao", debit).GetAwaiter().GetResult();
            if (!response.IsSuccessStatusCode)
            {
                return BadRequest(new { message = "Falha no débito", type = "INVALID_ACCOUNT" });
            }

            var credit = new { IdentificacaoRequisicao = req.IdentificacaoRequisicao, NumeroConta = req.ContaDestino, Valor = req.Valor, Tipo = "C" };
            response = client.PostAsJsonAsync($"{baseUrl}/api/conta/movimentacao", credit).GetAwaiter().GetResult();
            if (!response.IsSuccessStatusCode)
            {
                // estorno
                client.PostAsJsonAsync($"{baseUrl}/api/conta/movimentacao", new { IdentificacaoRequisicao = req.IdentificacaoRequisicao, Valor = req.Valor, Tipo = "C" }).GetAwaiter().GetResult();
                return BadRequest(new { message = "Falha no crédito", type = "INVALID_ACCOUNT" });
            }

            var id = Guid.NewGuid().ToString();
            _db.Execute("INSERT INTO transferencia (idtransferencia, idcontacorrente_origem, idcontacorrente_destino, datamovimento, valor) VALUES (@Id, @Origem, @Destino, @Data, @Valor)", new { Id = id, Origem = req.ContaOrigem, Destino = req.ContaDestino, Data = DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm:ss"), Valor = req.Valor });

            // Publish transfer event for downstream processing (tarifas)
            try
            {
                var payload = System.Text.Json.JsonSerializer.Serialize(new { IdRequisicao = req.IdentificacaoRequisicao, ContaOrigem = req.ContaOrigem });
                _producer.Produce("transferencias", new Confluent.Kafka.Message<Confluent.Kafka.Null, string> { Value = payload });
            }
            catch { /* best-effort, do not fail the API on publish error */ }

            return NoContent();
        }
    }

    public record TransferRequest(string IdentificacaoRequisicao, string ContaOrigem, string ContaDestino, double Valor);
}
