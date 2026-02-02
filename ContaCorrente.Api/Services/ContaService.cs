using System.Data;
using Dapper;
using BancoDigitalAna.ContaCorrente.Api.Repositories;

namespace BancoDigitalAna.ContaCorrente.Api.Services
{
    public class ContaService : IContaService
    {
        private readonly IDbConnection _db;
        private readonly ISecurityService _security;
        private readonly IJwtService _jwt;
        private readonly IIdempotenciaRepository _idemRepo;

        public ContaService(IDbConnection db, ISecurityService security, IJwtService jwt, IIdempotenciaRepository idemRepo)
        {
            _db = db;
            _security = security;
            _jwt = jwt;
            _idemRepo = idemRepo;
        }

        public async Task<(int Numero, string Id)> RegisterAsync(string cpf, string senha, string? nome)
        {
            if (!CpfValidator.IsValid(cpf)) throw new ArgumentException("CPF inválido", "INVALID_DOCUMENT");

            var id = Guid.NewGuid().ToString();
            var numero = new Random().Next(100000, 999999);

            var (hash, salt) = _security.HashPassword(senha);
            var sql = "INSERT INTO contacorrente (idcontacorrente, numero, cpf, nome, ativo, senha, salt) VALUES (@Id, @Numero, @Cpf, @Nome, 1, @Senha, @Salt)";
            await _db.ExecuteAsync(sql, new { Id = id, Numero = numero, Cpf = cpf, Nome = nome ?? "", Senha = hash, Salt = salt });

            return (numero, id);
        }

        public async Task<string?> LoginAsync(string login, string senha)
        {
            string sql;
            object param;
            if (int.TryParse(login, out int numero))
            {
                sql = "SELECT idcontacorrente, numero, senha, salt FROM contacorrente WHERE numero = @Numero";
                param = new { Numero = numero };
            }
            else
            {
                sql = "SELECT idcontacorrente, numero, senha, salt FROM contacorrente WHERE cpf = @Cpf";
                param = new { Cpf = login };
            }
            var acc = await _db.QueryFirstOrDefaultAsync(sql, param);
            if (acc == null) return null;

            var senhaHash = (string)acc.senha;
            var salt = (string)acc.salt;
            if (!_security.VerifyPassword(senha, senhaHash, salt)) return null;

            var token = _jwt.GenerateToken((string)acc.idcontacorrente, acc.numero.ToString());
            return token;
        }

        public async Task<bool> InactivateAsync(string accountId, string senha)
        {
            var sqlCheck = "SELECT senha, salt, ativo FROM contacorrente WHERE idcontacorrente = @Id";
            var acc = await _db.QueryFirstOrDefaultAsync(sqlCheck, new { Id = accountId });
            if (acc == null || (int)acc.ativo != 1) return false;

            var senhaHash = (string)acc.senha;
            var salt = (string)acc.salt;
            if (!_security.VerifyPassword(senha, senhaHash, salt)) return false;

            var sql = "UPDATE contacorrente SET ativo = 0 WHERE idcontacorrente = @Id";
            await _db.ExecuteAsync(sql, new { Id = accountId });
            return true;
        }

        public async Task<string?> GetAccountIdByNumeroAsync(string numero)
        {
            var acc = await _db.QueryFirstOrDefaultAsync("SELECT idcontacorrente FROM contacorrente WHERE numero = @Numero", new { Numero = numero });
            return acc?.idcontacorrente;
        }

        public async Task<bool> MovimentacaoAsync(string accountId, double valor, string tipo, string? identificacao)
        {
            if (valor <= 0) throw new ArgumentException("Valor inválido", "INVALID_VALUE");
            if (tipo != "C" && tipo != "D") throw new ArgumentException("Tipo inválido", "INVALID_TYPE");

            // Validate account
            var acc = await _db.QueryFirstOrDefaultAsync("SELECT ativo FROM contacorrente WHERE idcontacorrente = @Id", new { Id = accountId });
            if (acc == null) throw new ArgumentException("Conta inválida", "INVALID_ACCOUNT");
            if ((int)acc.ativo == 0) throw new ArgumentException("Conta inativa", "INACTIVE_ACCOUNT");

            // Idempotency
            if (!string.IsNullOrEmpty(identificacao) && _idemRepo.Exists(identificacao))
            {
                return true;
            }

            var sql = "INSERT INTO movimento (idmovimento, idcontacorrente, datamovimento, tipomovimento, valor) VALUES (@Id, @AccountId, @Data, @Tipo, @Valor)";
            await _db.ExecuteAsync(sql, new { Id = Guid.NewGuid().ToString(), AccountId = accountId, Data = DateTime.UtcNow.ToString("O"), Tipo = tipo, Valor = valor });

            if (!string.IsNullOrEmpty(identificacao))
            {
                _idemRepo.Save(identificacao, "OK");
            }

            return true;
        }

        public async Task<(int Numero, string Nome, string Saldo)> GetSaldoAsync(string accountId)
        {
            var acc = await _db.QueryFirstOrDefaultAsync("SELECT numero, nome, ativo FROM contacorrente WHERE idcontacorrente = @Id", new { Id = accountId });
            if (acc == null) throw new ArgumentException("Conta inválida", "INVALID_ACCOUNT");
            if ((int)acc.ativo == 0) throw new ArgumentException("Conta inativa", "INACTIVE_ACCOUNT");

            var credits = await _db.ExecuteScalarAsync<double?>("SELECT SUM(valor) FROM movimento WHERE idcontacorrente = @Id AND tipomovimento = 'C'", new { Id = accountId }) ?? 0;
            var debits = await _db.ExecuteScalarAsync<double?>("SELECT SUM(valor) FROM movimento WHERE idcontacorrente = @Id AND tipomovimento = 'D'", new { Id = accountId }) ?? 0;
            var saldo = credits - debits;

            return ((int)acc.numero, (string)acc.nome, saldo.ToString("F2"));
        }
    }
}