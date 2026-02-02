using System.Data;
using Dapper;
using Confluent.Kafka;
using BancoDigitalAna.Transferencia.Api.Models;

namespace BancoDigitalAna.Transferencia.Api.Services
{
    public class TransferenciaService : ITransferenciaService
    {
        private readonly IDbConnection _db;
        private readonly IHttpClientFactory _http;
        private readonly IProducer<Null, string> _producer;

        public TransferenciaService(IDbConnection db, IHttpClientFactory http, IProducer<Null, string> producer)
        {
            _db = db;
            _http = http;
            _producer = producer;
        }

        public async Task TransferAsync(TransferRequest req, string accountNumber, string authHeader)
        {
            // Basic validations
            if (req.Valor <= 0) throw new ArgumentException("Valor inválido", "INVALID_VALUE");

            // Verify the JWT account matches the origin account
            if (accountNumber != req.ContaOrigem) throw new UnauthorizedAccessException();

            // Get account IDs
            var origemId = await _db.QueryFirstOrDefaultAsync<string>("SELECT idcontacorrente FROM contacorrente WHERE numero = @Numero", new { Numero = req.ContaOrigem });
            var destinoId = await _db.QueryFirstOrDefaultAsync<string>("SELECT idcontacorrente FROM contacorrente WHERE numero = @Numero", new { Numero = req.ContaDestino });
            if (origemId == null || destinoId == null) throw new ArgumentException("Conta inválida", "INVALID_ACCOUNT");

            // Call ContaCorrente API to debit
            var client = _http.CreateClient();
            if (!string.IsNullOrEmpty(authHeader))
            {
                client.DefaultRequestHeaders.Add("Authorization", authHeader);
            }
            var baseUrl = Environment.GetEnvironmentVariable("CONTA_BASE_URL") ?? "http://contacorrente:5000";

            var debit = new { IdentificacaoRequisicao = req.IdentificacaoRequisicao, Valor = req.Valor, Tipo = "D" };
            var response = await client.PostAsJsonAsync($"{baseUrl}/api/conta/movimentacao", debit);
            if (!response.IsSuccessStatusCode)
            {
                throw new ArgumentException("Falha no débito", "INVALID_ACCOUNT");
            }

            var credit = new { IdentificacaoRequisicao = req.IdentificacaoRequisicao, NumeroConta = req.ContaDestino, Valor = req.Valor, Tipo = "C" };
            response = await client.PostAsJsonAsync($"{baseUrl}/api/conta/movimentacao", credit);
            if (!response.IsSuccessStatusCode)
            {
                // estorno
                await client.PostAsJsonAsync($"{baseUrl}/api/conta/movimentacao", new { IdentificacaoRequisicao = req.IdentificacaoRequisicao, Valor = req.Valor, Tipo = "C" });
                throw new ArgumentException("Falha no crédito", "INVALID_ACCOUNT");
            }

            var id = Guid.NewGuid().ToString();
            await _db.ExecuteAsync("INSERT INTO transferencia (idtransferencia, idcontacorrente_origem, idcontacorrente_destino, datamovimento, valor) VALUES (@Id, @Origem, @Destino, @Data, @Valor)", new { Id = id, Origem = origemId, Destino = destinoId, Data = DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm:ss"), Valor = req.Valor });

            // Publish transfer event for downstream processing (tarifas)
            try
            {
                var payload = System.Text.Json.JsonSerializer.Serialize(new { IdRequisicao = req.IdentificacaoRequisicao, ContaOrigem = req.ContaOrigem });
                _producer.Produce("transferencias", new Confluent.Kafka.Message<Confluent.Kafka.Null, string> { Value = payload });
            }
            catch { /* best-effort, do not fail the API on publish error */ }
        }
    }
}