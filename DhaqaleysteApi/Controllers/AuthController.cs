using DhaqaleysteApi.Data;
using DhaqaleysteApi.Dtos;
using DhaqaleysteApi.Models;
using DhaqaleysteApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace DhaqaleysteApi.Controllers
{
    /// <summary>Staff sign-in. Customers never sign in; there is no public registration.</summary>
    [Route("api/auth")]
    public class AuthController : ApiControllerBase
    {
        private const int MaxFailedAttempts = 5;
        private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

        private readonly AppDbContext _db;
        private readonly IPasswordHasher<User> _hasher;
        private readonly TokenService _tokens;
        private readonly ILogger<AuthController> _logger;

        public AuthController(AppDbContext db, IPasswordHasher<User> hasher, TokenService tokens, ILogger<AuthController> logger)
        {
            _db = db;
            _hasher = hasher;
            _tokens = tokens;
            _logger = logger;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        [EnableRateLimiting(RateLimitPolicies.Auth)]
        public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
        {
            var identifier = request.EmailOrPhone.Trim();
            User? user = null;

            if (identifier.Length > 0)
            {
                user = identifier.Contains('@')
                    ? await _db.Users.FirstOrDefaultAsync(x => x.Email == identifier.ToLowerInvariant())
                    : await _db.Users.FirstOrDefaultAsync(x => x.Phone == identifier);
            }

            if (user == null || string.IsNullOrEmpty(user.PasswordHash))
            {
                // Spend the same time as a real check so response timing does not reveal which accounts exist.
                _hasher.HashPassword(new User(), request.Password);
                return InvalidLogin();
            }

            if (user.LockoutEndUtc > DateTime.UtcNow)
            {
                _logger.LogWarning("Login attempt for locked account {UserId}", user.UserId);
                return InvalidLogin();
            }

            var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
            if (result == PasswordVerificationResult.Failed)
            {
                user.AccessFailedCount++;
                if (user.AccessFailedCount >= MaxFailedAttempts)
                {
                    user.LockoutEndUtc = DateTime.UtcNow.Add(LockoutDuration);
                    user.AccessFailedCount = 0;
                    _logger.LogWarning("Account {UserId} locked after repeated failed logins", user.UserId);
                }
                await _db.SaveChangesAsync();
                return InvalidLogin();
            }

            if (!user.IsActive || !Roles.IsAssignable(user.Role))
                return InvalidLogin();

            if (result == PasswordVerificationResult.SuccessRehashNeeded)
                user.PasswordHash = _hasher.HashPassword(user, request.Password);

            user.AccessFailedCount = 0;
            user.LockoutEndUtc = null;
            user.LastLoginAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            _logger.LogInformation("Staff {UserId} signed in", user.UserId);
            return await _tokens.IssueAsync(user, ClientIp);
        }

        [HttpPost("refresh")]
        [AllowAnonymous]
        [EnableRateLimiting(RateLimitPolicies.Auth)]
        public async Task<ActionResult<AuthResponse>> Refresh(RefreshRequest request)
        {
            var response = await _tokens.RefreshAsync(request.RefreshToken, ClientIp);
            if (response == null)
                return ProblemResult(StatusCodes.Status401Unauthorized, "Your session has expired. Please sign in again.");
            return response;
        }

        [HttpPost("logout")]
        [AllowAnonymous]
        public async Task<IActionResult> Logout(RefreshRequest request)
        {
            await _tokens.RevokeAsync(request.RefreshToken);
            return NoContent();
        }

        [HttpGet("me")]
        [Authorize(Policy = AuthPolicies.Staff)]
        public async Task<ActionResult<StaffDto>> Me()
        {
            var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == CurrentUserId);
            return user == null ? NotFound() : StaffDto.From(user);
        }

        [HttpPost("change-password")]
        [Authorize(Policy = AuthPolicies.Staff)]
        [EnableRateLimiting(RateLimitPolicies.Auth)]
        public async Task<IActionResult> ChangePassword(ChangePasswordRequest request)
        {
            var user = await _db.Users.FirstOrDefaultAsync(x => x.UserId == CurrentUserId);
            if (user == null)
                return NotFound();

            if (_hasher.VerifyHashedPassword(user, user.PasswordHash, request.CurrentPassword) == PasswordVerificationResult.Failed)
                return ValidationError(nameof(request.CurrentPassword), "Current password is incorrect.");

            var error = PasswordRules.Validate(request.NewPassword);
            if (error != null)
                return ValidationError(nameof(request.NewPassword), error);

            user.PasswordHash = _hasher.HashPassword(user, request.NewPassword);
            user.SecurityStamp = Guid.NewGuid().ToString("N");
            user.UpdatedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            await _tokens.RevokeAllAsync(user.UserId);

            return NoContent();
        }

        private ObjectResult InvalidLogin() =>
            ProblemResult(StatusCodes.Status401Unauthorized, "Invalid email/phone or password.");
    }
}
