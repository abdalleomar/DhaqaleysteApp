using System.Security.Cryptography;
using System.Text;

namespace DhaqaleysteApi.Services
{
    /// <summary>Staff invite codes such as "K7QM-29XF" (no 0/O or 1/I, so they are easy to read out).</summary>
    public static class StaffInviteCodes
    {
        private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

        public static string Generate()
        {
            var chars = new char[8];
            for (var i = 0; i < chars.Length; i++)
                chars[i] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)];
            return new string(chars, 0, 4) + "-" + new string(chars, 4, 4);
        }

        /// <summary>Upper-case, without spaces and dashes, so "k7qm 29xf" matches "K7QM-29XF".</summary>
        public static string Normalize(string? code) =>
            new string((code ?? "").Where(char.IsLetterOrDigit).Select(char.ToUpperInvariant).ToArray());

        /// <summary>Constant-time comparison of a typed code with the current code.</summary>
        public static bool Matches(string? typed, string current)
        {
            var a = Normalize(typed);
            var b = Normalize(current);
            if (a.Length == 0 || b.Length == 0)
                return false;
            return CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(a), Encoding.UTF8.GetBytes(b));
        }
    }
}
