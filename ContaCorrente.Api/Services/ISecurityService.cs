namespace BancoDigitalAna.ContaCorrente.Api.Services
{
    public interface ISecurityService
    {
        (string Hash, string Salt) HashPassword(string password);
        bool VerifyPassword(string password, string hashBase64, string saltBase64);
    }
}