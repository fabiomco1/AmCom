using Microsoft.Data.Sqlite;
using System.IO;

var dbPath = "../contacorrente.db";
var sqlPath = "../ContaCorrente.Api/sql/contacorrente.sql";

if (!File.Exists(dbPath))
{
    using var connection = new SqliteConnection($"Data Source={dbPath}");
    connection.Open();

    var sql = File.ReadAllText(sqlPath);
    using var command = new SqliteCommand(sql, connection);
    command.ExecuteNonQuery();

    Console.WriteLine("Database initialized.");
}
else
{
    Console.WriteLine("Database already exists.");
}