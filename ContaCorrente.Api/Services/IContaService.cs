namespace BancoDigitalAna.ContaCorrente.Api.Services
{
    public interface IContaService
    {
        Task<(int Numero, string Id)> RegisterAsync(string cpf, string senha, string? nome);
        Task<string?> LoginAsync(string login, string senha);
        Task<bool> InactivateAsync(string accountId, string senha);
        Task<string?> GetAccountIdByNumeroAsync(string numero);
        Task<bool> MovimentacaoAsync(string accountId, double valor, string tipo, string? identificacao);
        Task<(int Numero, string Nome, string Saldo)> GetSaldoAsync(string accountId);
    }
}