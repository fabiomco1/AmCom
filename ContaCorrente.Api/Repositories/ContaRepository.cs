using System.Data;
using Dapper;

namespace BancoDigitalAna.ContaCorrente.Api.Repositories
{
    public class ContaRepository
    {
        private readonly IDbConnection _db;
        public ContaRepository(IDbConnection db) { _db = db; }

        // Placeholder methods - controller uses direct Dapper for simplicity
    }
}
