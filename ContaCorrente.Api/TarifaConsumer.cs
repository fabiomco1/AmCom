using Confluent.Kafka;
using Dapper;
using System.Text.Json;
using Microsoft.Extensions.Hosting;

namespace BancoDigitalAna.ContaCorrente.Api
{
    public class TarifaConsumer : BackgroundService
    {
        private readonly ConsumerConfig _config;
        private readonly System.Data.IDbConnection _db;

        public TarifaConsumer(ConsumerConfig config, System.Data.IDbConnection db)
        {
            _config = config;
            _db = db;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return Task.Run(() =>
            {
                using var consumer = new ConsumerBuilder<Ignore, string>(_config).Build();
                consumer.Subscribe("tarifas");
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        var cr = consumer.Consume(stoppingToken);
                        var data = JsonSerializer.Deserialize<Dictionary<string, object>>(cr.Message.Value);
                        var conta = data.GetValueOrDefault("Conta")?.ToString() ?? data.GetValueOrDefault("conta")?.ToString();
                        var valor = Convert.ToDouble(data.GetValueOrDefault("Valor") ?? data.GetValueOrDefault("valor") ?? 0);

                        if (!string.IsNullOrEmpty(conta))
                        {
                            // Persist as movimento (debito)
                            var idmov = Guid.NewGuid().ToString();
                            _db.Execute("INSERT INTO movimento (idmovimento, idcontacorrente, datamovimento, tipomovimento, valor) VALUES (@Id, @Conta, @Data, 'D', @Valor)", new { Id = idmov, Conta = conta, Data = DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm:ss"), Valor = valor });
                        }
                    }
                    catch (OperationCanceledException) { break; }
                    catch { /* ignore */ }
                }
            }, stoppingToken);
        }
    }
}
