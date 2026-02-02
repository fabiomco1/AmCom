namespace BancoDigitalAna.ContaCorrente.Api.Repositories
{
    public interface IIdempotenciaRepository
    {
        bool Exists(string chave);
        string? GetResultado(string chave);
        void Save(string chave, string resultado);
    }
}