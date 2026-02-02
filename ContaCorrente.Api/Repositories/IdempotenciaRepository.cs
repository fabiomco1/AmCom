using System.Data;
using Dapper;

namespace BancoDigitalAna.ContaCorrente.Api.Repositories
{
    public class IdempotenciaRepository : IIdempotenciaRepository
    {
        private readonly IDbConnection _db;
        public IdempotenciaRepository(IDbConnection db) { _db = db; }

        public bool Exists(string chave) => _db.QueryFirstOrDefault<int?>("SELECT 1 FROM idempotencia WHERE chave_idempotencia = @Chave", new { Chave = chave }) == 1;

        public void Save(string chave, string resultado)
        {
            _db.Execute("INSERT OR REPLACE INTO idempotencia (chave_idempotencia, requisicao, resultado) VALUES (@Chave, '', @Res)", new { Chave = chave, Res = resultado });
        }

        public string? GetResultado(string chave) => _db.QueryFirstOrDefault<string>("SELECT resultado FROM idempotencia WHERE chave_idempotencia = @Chave", new { Chave = chave });
    }
}
