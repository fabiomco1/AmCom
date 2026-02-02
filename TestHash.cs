using System.Security.Cryptography;

var hashBase64 = "cQNxUXePdTuDWnz3TfHaYuZlfeq2128N59RejKTx5WQ=";
var saltBase64 = "1zGnoPhDBIZgRo5iQEmfjA==";
var password = "123456";

var salt = Convert.FromBase64String(saltBase64);
using var rfc = new Rfc2898DeriveBytes(password, salt, 100000, HashAlgorithmName.SHA256);
var key = rfc.GetBytes(32);
var computed = Convert.ToBase64String(key);
Console.WriteLine($"Computed: {computed}");
Console.WriteLine($"Stored: {hashBase64}");
Console.WriteLine($"Match: {CryptographicOperations.FixedTimeEquals(Convert.FromBase64String(computed), Convert.FromBase64String(hashBase64))}");