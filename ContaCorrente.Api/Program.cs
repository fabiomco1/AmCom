using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Dapper;
using Microsoft.Data.Sqlite;
using BancoDigitalAna.ContaCorrente.Api.Services;
using BancoDigitalAna.ContaCorrente.Api.Repositories;
using BancoDigitalAna.ContaCorrente.Api;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

// Configuration
var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSection.GetValue<string>("Key") ?? "VeryLongDevelopmentJwtSecretKey_ChangeForProd_0123456789ABCDEFG";
var key = Encoding.ASCII.GetBytes(jwtKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = false,
            ValidateAudience = false,
            ClockSkew = TimeSpan.Zero
        };
    });

// Database connection factory
builder.Services.AddSingleton<System.Data.IDbConnection>(_ =>
{
    var conn = new SqliteConnection(builder.Configuration.GetConnectionString("Default"));
    conn.Open();
    return conn;
});

// Simple services
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IContaService, ContaService>();
builder.Services.AddSingleton<ISecurityService, SecurityService>();
builder.Services.AddScoped<IIdempotenciaRepository, IdempotenciaRepository>();

// Idempotency middleware will be added to pipeline via extension
builder.Services.AddMemoryCache();

// Kafka consumer config for tarifa handling (optional)
var kafkaBootstrap = builder.Configuration.GetValue<string>("Kafka:BootstrapServers") ?? "kafka:9092";
var consumerConfig = new Confluent.Kafka.ConsumerConfig
{
    BootstrapServers = kafkaBootstrap,
    GroupId = "contacorrente-tarifa",
    AutoOffsetReset = Confluent.Kafka.AutoOffsetReset.Earliest
};
builder.Services.AddSingleton(consumerConfig);
if (builder.Configuration.GetValue<bool>("EnableKafka", false))
{
    builder.Services.AddHostedService<TarifaConsumer>();
}

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

// Expose Program class for integration tests
public partial class Program { }
