using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using DhaqaleysteApi.Data;
using DhaqaleysteApi.Dtos;
using DhaqaleysteApi.Models;
using DhaqaleysteApi.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace DhaqaleysteApi.Services
{
    public class TokenService
    {
        public const string SecurityStampClaim = "stamp";
        public const string RoleClaim = "role";
        public const string NameClaim = "name";

        private readonly AppDbContext _db;
        private readonly JwtOptions _options;

        public TokenService(AppDbContext db, IOptions<JwtOptions> options)
        {
            _db = db;
            _options = options.Value;
        }

        public static SymmetricSecurityKey CreateKey(string signingKey) =>
            new(Encoding.UTF8.GetBytes(signingKey));

        /// <summary>Issues a new access token and refresh token, and saves the refresh token hash.</summary>
        public async Task<AuthResponse> IssueAsync(User user, string? ip)
        {
            var now = DateTime.UtcNow;
            var accessExpires = now.AddMinutes(_options.AccessTokenMinutes);

            var descriptor = new SecurityTokenDescriptor
            {
                Issuer = _options.Issuer,
                Audience = _options.Audience,
                IssuedAt = now,
                NotBefore = now,
                Expires = accessExpires,
                SigningCredentials = new SigningCredentials(
                    CreateKey(_options.SigningKey), SecurityAlgorithms.HmacSha256),
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
                    new Claim(JwtRegisteredClaimNames.Name, user.FullName),
                    new Claim(RoleClaim, user.Role),
                    new Claim(SecurityStampClaim, user.SecurityStamp),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
                })
            };

            var accessToken = new JsonWebTokenHandler().CreateToken(descriptor);

            var rawRefresh = GenerateRefreshToken();
            var refresh = new RefreshToken
            {
                UserId = user.UserId,
                TokenHash = Hash(rawRefresh),
                CreatedAtUtc = now,
                ExpiresAtUtc = now.AddDays(_options.RefreshTokenDays),
                CreatedByIp = ip
            };
            _db.RefreshTokens.Add(refresh);
            await _db.SaveChangesAsync();

            return new AuthResponse
            {
                AccessToken = accessToken,
                AccessTokenExpiresAtUtc = accessExpires,
                RefreshToken = rawRefresh,
                RefreshTokenExpiresAtUtc = refresh.ExpiresAtUtc,
                User = StaffDto.From(user)
            };
        }

        /// <summary>
        /// Rotates a refresh token. Reusing an already-rotated token revokes every token for that user,
        /// because it means the token was stolen.
        /// </summary>
        public async Task<AuthResponse?> RefreshAsync(string rawToken, string? ip)
        {
            var hash = Hash(rawToken);
            var token = await _db.RefreshTokens
                .Include(x => x.User)
                .FirstOrDefaultAsync(x => x.TokenHash == hash);

            if (token?.User == null)
                return null;

            if (token.RevokedAtUtc != null)
            {
                await RevokeAllAsync(token.UserId);
                return null;
            }

            var user = token.User;
            if (DateTime.UtcNow >= token.ExpiresAtUtc || !user.IsActive || !Roles.IsAssignable(user.Role))
                return null;

            var response = await IssueAsync(user, ip);
            token.RevokedAtUtc = DateTime.UtcNow;
            token.ReplacedByTokenHash = Hash(response.RefreshToken);
            await _db.SaveChangesAsync();
            return response;
        }

        public async Task RevokeAsync(string rawToken)
        {
            var hash = Hash(rawToken);
            var token = await _db.RefreshTokens.FirstOrDefaultAsync(x => x.TokenHash == hash);
            if (token != null && token.RevokedAtUtc == null)
            {
                token.RevokedAtUtc = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }
        }

        public Task RevokeAllAsync(int userId) =>
            _db.RefreshTokens
                .Where(x => x.UserId == userId && x.RevokedAtUtc == null)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.RevokedAtUtc, DateTime.UtcNow));

        private static string GenerateRefreshToken() =>
            Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(64));

        public static string Hash(string value) =>
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    }
}
