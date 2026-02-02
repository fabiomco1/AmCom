namespace BancoDigitalAna.ContaCorrente.Api.Services
{
    public static class CpfValidator
    {
        public static bool IsValid(string cpf)
        {
            if (string.IsNullOrWhiteSpace(cpf)) return false;
            var digits = new string(cpf.Where(char.IsDigit).ToArray());
            if (digits.Length != 11) return false;
            if (new string(digits[0], 11) == digits) return false;

            int[] numbers = digits.Select(c => c - '0').ToArray();

            // first check digit
            int sum1 = 0;
            for (int i = 0; i < 9; i++) sum1 += numbers[i] * (10 - i);
            int rem1 = sum1 % 11;
            int dig1 = rem1 < 2 ? 0 : 11 - rem1;
            if (dig1 > 9) dig1 = 0;
            if (numbers[9] != dig1) return false;

            // second check digit
            int sum2 = 0;
            for (int i = 0; i < 10; i++) sum2 += numbers[i] * (11 - i);
            int rem2 = sum2 % 11;
            int dig2 = rem2 < 2 ? 0 : 11 - rem2;
            if (dig2 > 9) dig2 = 0;
            if (numbers[10] != dig2) return false;

            return true;
        }
    }
}
