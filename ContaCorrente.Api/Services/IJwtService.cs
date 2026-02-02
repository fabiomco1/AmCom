namespace BancoDigitalAna.ContaCorrente.Api.Services
{
    public interface IJwtService
    {
        string GenerateToken(string accountId, string accountNumber);
    }
}