using BancoDigitalAna.ContaCorrente.Api.Services;
using Xunit;

namespace BancoDigitalAna.Tests;

public class CpfAndSecurityTests
{
    [Theory]
    [InlineData("111.444.777-35", true)]
    [InlineData("12345678909", true)]
    [InlineData("00000000000", false)]
    public void CpfValidator_Works(string cpf, bool expected)
    {
        var ok = CpfValidator.IsValid(cpf);
        Assert.Equal(expected, ok);
    }

    [Fact]
    public void SecurityService_HashAndVerify()
    {
        var svc = new SecurityService();
        var (hash, salt) = svc.HashPassword("MinhaSenhaSegura123!");
        Assert.NotNull(hash);
        Assert.NotNull(salt);
        Assert.True(svc.VerifyPassword("MinhaSenhaSegura123!", hash, salt));
        Assert.False(svc.VerifyPassword("Wrong", hash, salt));
    }
}
