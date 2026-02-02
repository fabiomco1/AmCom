using Confluent.Kafka;
using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

var builder = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        var kafkaConfig = new ConsumerConfig
        {
            BootstrapServers = context.Configuration.GetValue<string>("Kafka:BootstrapServers") ?? "kafka:9092",
            GroupId = "tarifa-service-group",
            AutoOffsetReset = AutoOffsetReset.Earliest
        };

        services.AddSingleton(kafkaConfig);
        services.AddSingleton<IHostedService, TarifaWorker>();
        services.AddSingleton<System.Data.IDbConnection>(_ => {
            var conn = new SqliteConnection(context.Configuration.GetConnectionString("Default") ?? "Data Source=/data/contacorrente.db");
            conn.Open();
            return conn;
        });
    })
    .Build();

await builder.RunAsync();
