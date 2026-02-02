using System.Text.Json;
using Confluent.Kafka;
using Dapper;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

public class TarifaWorker : BackgroundService
{
    private readonly ConsumerConfig _config;
    private readonly IDbConnection _db;

    public TarifaWorker(ConsumerConfig config, IDbConnection db)
    {
        _config = config;
        _db = db;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.Run(() =>
        {
            using var consumer = new ConsumerBuilder<Ignore, string>(_config).Build();
            consumer.Subscribe("transferencias");
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var cr = consumer.Consume(stoppingToken);
                    var data = JsonSerializer.Deserialize<Dictionary<string, string>>(cr.Message.Value);
                    var idReq = data.GetValueOrDefault("IdRequisicao");
                    var conta = data.GetValueOrDefault("ContaOrigem");

                    // Compute tarifa from env or config
                    var valorTarifa = 2.0; // default

                    var idTarifa = Guid.NewGuid().ToString();
                    _db.Execute("INSERT INTO tarifa (idtarifa, idcontacorrente, datamovimento, valor) VALUES (@Id, @Conta, @Data, @Valor)", new { Id = idTarifa, Conta = conta, Data = DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm:ss"), Valor = valorTarifa });

                    // Produce tarifas-realizadas event (best-effort)
                    var prodConfig = new ProducerConfig { BootstrapServers = _config.BootstrapServers };
                    using var producer = new ProducerBuilder<Null, string>(prodConfig).Build();
                    var payload = JsonSerializer.Serialize(new { IdTarifa = idTarifa, Conta = conta, Valor = valorTarifa });
                    producer.Produce("tarifas", new Message<Null, string> { Value = payload });
                }
                catch (OperationCanceledException) { break; }
                catch { /* ignore and continue */ }
            }
        }, stoppingToken);
    }
}
