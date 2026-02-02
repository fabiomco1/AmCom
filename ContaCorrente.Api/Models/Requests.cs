namespace BancoDigitalAna.ContaCorrente.Api.Models
{
    public record RegisterRequest(string Cpf, string Senha, string? Nome);
    public record LoginRequest(string Login, string Senha);
    public record InactivateRequest(string senha);
    public record MovimentacaoRequest(string? IdentificacaoRequisicao, string? NumeroConta, double Valor, string Tipo);
}
