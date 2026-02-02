namespace BancoDigitalAna.Transferencia.Api.Models
{
    public record TransferRequest(string IdentificacaoRequisicao, string ContaOrigem, string ContaDestino, double Valor);
}