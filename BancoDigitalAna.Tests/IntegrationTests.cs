using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Text;
using System.Net.Http;
using System;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using Dapper;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Xunit;

public class IntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private WebApplicationFactory<Program> CreateFactory()
    {
        var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration(config =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["Jwt:Key"] = "VeryLongDevelopmentJwtSecretKey_ChangeForProd_0123456789ABCDEFG"
                });
            });
            builder.ConfigureServices(services =>
            {
                // Replace IDbConnection with an in-memory shared Sqlite connection and initialize schema
                services.RemoveAll<System.Data.IDbConnection>();
                // Remove hosted services (Kafka consumers/producers) in test environment
                services.RemoveAll<Microsoft.Extensions.Hosting.IHostedService>();
                var conn = new SqliteConnection("Data Source=:memory:;Cache=Shared");
                conn.Open();

                var schema = @"
CREATE TABLE IF NOT EXISTS contacorrente (
    idcontacorrente TEXT PRIMARY KEY,
    numero INTEGER NOT NULL UNIQUE,
    cpf TEXT NOT NULL UNIQUE,
    nome TEXT NOT NULL,
    ativo INTEGER NOT NULL DEFAULT 1,
    senha TEXT NOT NULL,
    salt TEXT NOT NULL
);
CREATE TABLE IF NOT EXISTS movimento (
    idmovimento TEXT PRIMARY KEY,
    idcontacorrente TEXT NOT NULL,
    datamovimento TEXT NOT NULL,
    tipomovimento TEXT NOT NULL,
    valor REAL NOT NULL
);
CREATE TABLE IF NOT EXISTS idempotencia (
    chave_idempotencia TEXT PRIMARY KEY,
    requisicao TEXT,
    resultado TEXT
);
CREATE TABLE IF NOT EXISTS transferencia (
    idtransferencia TEXT PRIMARY KEY,
    idcontacorrente_origem TEXT NOT NULL,
    idcontacorrente_destino TEXT NOT NULL,
    datamovimento TEXT NOT NULL,
    valor REAL NOT NULL
);
CREATE TABLE IF NOT EXISTS tarifa (
    idtarifa TEXT PRIMARY KEY,
    idcontacorrente TEXT NOT NULL,
    datamovimento TEXT NOT NULL,
    valor REAL NOT NULL
);
";

                conn.Execute(schema);
                services.AddSingleton<System.Data.IDbConnection>(conn);
            });
        });

        return factory;
    }

    [Fact]
    public async Task Register_Login_Movimentacao_Saldo_Workflow()
    {
        using var factory = CreateFactory();
        var client = factory.CreateClient();

        // Register
        var register = new { Cpf = "11144477735", Senha = "Senha123!", Nome = "Teste" };
        var regResp = await client.PostAsync("/api/conta/register", new StringContent(JsonSerializer.Serialize(register), Encoding.UTF8, "application/json"));
        Assert.Equal(System.Net.HttpStatusCode.Created, regResp.StatusCode);
        var regBody = JsonSerializer.Deserialize<JsonElement>(await regResp.Content.ReadAsStringAsync());
        var numero = regBody.GetProperty("numero").GetInt32();

        // Login
        var login = new { Login = numero.ToString(), Senha = "Senha123!" };
        var loginResp = await client.PostAsync("/api/conta/login", new StringContent(JsonSerializer.Serialize(login), Encoding.UTF8, "application/json"));
        Assert.Equal(System.Net.HttpStatusCode.OK, loginResp.StatusCode);
        var token = JsonSerializer.Deserialize<JsonElement>(await loginResp.Content.ReadAsStringAsync()).GetProperty("token").GetString();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Movimentacao (credito)
        var mov = new { IdentificacaoRequisicao = Guid.NewGuid().ToString(), Valor = 150.0, Tipo = "C" };
        var movResp = await client.PostAsync("/api/conta/movimentacao", new StringContent(JsonSerializer.Serialize(mov), Encoding.UTF8, "application/json"));
        Assert.Equal(System.Net.HttpStatusCode.NoContent, movResp.StatusCode);

        // Saldo
        var saldoResp = await client.GetAsync("/api/conta/saldo");
        Assert.Equal(System.Net.HttpStatusCode.OK, saldoResp.StatusCode);
        var saldoBody = JsonSerializer.Deserialize<JsonElement>(await saldoResp.Content.ReadAsStringAsync());
        var saldoStr = saldoBody.GetProperty("saldo").GetString();
        Assert.Equal("150.00", saldoStr);
    }

    [Fact]
    public async Task Inactivate_Account()
    {
        using var factory = CreateFactory();
        var client = factory.CreateClient();

        // Register
        var register = new { Cpf = "11144477735", Senha = "Senha123!", Nome = "Teste" };
        var regResp = await client.PostAsync("/api/conta/register", new StringContent(JsonSerializer.Serialize(register), Encoding.UTF8, "application/json"));
        Assert.Equal(System.Net.HttpStatusCode.Created, regResp.StatusCode);

        // Login
        var login = new { Login = "11144477735", Senha = "Senha123!" };
        var loginResp = await client.PostAsync("/api/conta/login", new StringContent(JsonSerializer.Serialize(login), Encoding.UTF8, "application/json"));
        Assert.Equal(System.Net.HttpStatusCode.OK, loginResp.StatusCode);
        var token = JsonSerializer.Deserialize<JsonElement>(await loginResp.Content.ReadAsStringAsync()).GetProperty("token").GetString();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Inactivate
        var inactivate = new { Senha = "Senha123!" };
        var inactResp = await client.PostAsync("/api/conta/inactivate", new StringContent(JsonSerializer.Serialize(inactivate), Encoding.UTF8, "application/json"));
        Assert.Equal(System.Net.HttpStatusCode.NoContent, inactResp.StatusCode);

        // Try to get saldo after inactivate - should fail
        var saldoResp = await client.GetAsync("/api/conta/saldo");
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, saldoResp.StatusCode);
    }
