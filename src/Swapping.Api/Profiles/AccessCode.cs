using System.Security.Cryptography;
using System.Text;

namespace Swapping.Api.Profiles;

/// <summary>
/// The access code: 16 Crockford Base32 symbols (80 random bits), shown as <c>XXXX-XXXX-XXXX-XXXX</c>.
/// The server keeps only the SHA-256 of the normalized code (assumption A3).
/// </summary>
public static class AccessCode
{
    public const string Alphabet = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";
    public const int Symbols = 16;

    public static string Generate()
    {
        Span<byte> bytes = stackalloc byte[Symbols * 5 / 8]; // 10 bytes = 80 bits
        RandomNumberGenerator.Fill(bytes);

        var text = new StringBuilder(Symbols + 3);
        for (var i = 0; i < Symbols; i++)
        {
            if (i > 0 && i % 4 == 0)
                text.Append('-');
            text.Append(Alphabet[ReadFiveBits(bytes, i * 5)]);
        }
        return text.ToString();
    }

    /// <summary>Uppercase, without dashes and spaces, with O → 0 and I/L → 1 (Crockford rules).</summary>
    public static string Normalize(string? input)
    {
        if (string.IsNullOrEmpty(input))
            return "";

        var text = new StringBuilder(input.Length);
        foreach (var c in input)
        {
            switch (char.ToUpperInvariant(c))
            {
                case '-' or ' ':
                    break;
                case 'O':
                    text.Append('0');
                    break;
                case 'I' or 'L':
                    text.Append('1');
                    break;
                case var upper:
                    text.Append(upper);
                    break;
            }
        }
        return text.ToString();
    }

    public static bool IsWellFormed(string? input)
    {
        var normalized = Normalize(input);
        return normalized.Length == Symbols && normalized.All(c => Alphabet.Contains(c));
    }

    public static byte[] Hash(string code) => SHA256.HashData(Encoding.UTF8.GetBytes(Normalize(code)));

    /// <summary>Constant-time comparison of the hash of <paramref name="input"/> with the stored hash.</summary>
    public static bool Matches(string? input, byte[] storedHash) =>
        IsWellFormed(input) && CryptographicOperations.FixedTimeEquals(Hash(input!), storedHash);

    private static int ReadFiveBits(ReadOnlySpan<byte> bytes, int bitOffset)
    {
        var value = 0;
        for (var b = 0; b < 5; b++)
        {
            var bit = bitOffset + b;
            value = (value << 1) | ((bytes[bit / 8] >> (7 - bit % 8)) & 1);
        }
        return value;
    }
}
