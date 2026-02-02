using BancoDigitalAna.Transferencia.Api.Models;
using System.Data;

namespace BancoDigitalAna.Transferencia.Api.Services
{
    public interface ITransferenciaService
    {
        Task TransferAsync(TransferRequest req, string accountNumber, string authHeader);
    }
}