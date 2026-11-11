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
        private readonly IEmailSender _email;

        private const int ResetCodeMinutes = 15;
        private const int MaxResetAttempts = 5;

        public AuthController(AppDbContext db, IPasswordHasher<User> hasher, TokenService tokens, ILogger<AuthController> logger, IEmailSender email)
        {
            _db = db;
            _hasher = hasher;
            _tokens = tokens;
            _logger = logger;
            _email = email;
        }

        /// <summary>
        /// A new staff member creates their own account with the invite code the admin shared.
        /// New accounts are always "Staff"; only an admin can make someone an admin.
        /// </summary>
        [HttpPost("register")]
        [AllowAnonymous]
        [EnableRateLimiting(RateLimitPolicies.Auth)]
        public async Task<ActionResult<AuthResponse>> Register(RegisterStaffRequest request)
        {
            var company = await _db.CompanySettings.AsNoTracking().FirstOrDefaultAsync();
            if (company == null || !StaffInviteCodes.Matches(request.InviteCode, company.StaffInviteCode))
            {
                _logger.LogWarning("Staff sign-up with a wrong or disabled invite code from {Ip}", ClientIp);
                return ValidationError(nameof(request.InviteCode), "The staff invite code is not correct. Ask your admin for the current code.");
            }

            var passwordError = PasswordRules.Validate(request.Password);
            if (passwordError != null)
                return ValidationError(nameof(request.Password), passwordError);

            var email = request.Email.Trim().ToLowerInvariant();
            var phone = request.Phone?.Trim() ?? "";
            if (await _db.Users.AnyAsync(u => u.Email == email))
                return ValidationError(nameof(request.Email), "An account with this email already exists. Sign in, or use Forgot password.");
            if (phone.Length > 0 && await _db.Users.AnyAsync(u => u.Phone == phone))
                return ValidationError(nameof(request.Phone), "An account with this phone number already exists.");

            var user = new User
            {
                FullName = request.FullName.Trim(),
                Email = email,
                Phone = phone,
                Role = Roles.Staff,
                IsActive = true,
                LastLoginAtUtc = DateTime.UtcNow
            };
            user.PasswordHash = _hasher.HashPassword(user, request.Password);
            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            _logger.LogInformation("Staff {UserId} created their own account with the invite code", user.UserId);
            return await _tokens.IssueAsync(user, ClientIp);
        }

        /// <summary>
        /// Emails a 6-digit code for resetting the password. Always answers the same way, so it never
        /// reveals which emails have accounts.
        /// </summary>
        [HttpPost("forgot-password")]
        [AllowAnonymous]
        [EnableRateLimiting(RateLimitPolicies.Auth)]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request, CancellationToken ct)
        {
            var email = request.Email.Trim().ToLowerInvariant();
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);

            // Only active staff accounts get a code; a new code at most once a minute.
            var recentlySent = user?.PasswordResetExpiresUtc > DateTime.UtcNow.AddMinutes(ResetCodeMinutes - 1);
            if (user != null && user.IsActive && Roles.IsAssignable(user.Role) && !recentlySent)
            {
                var code = System.Security.Cryptography.RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
                user.PasswordResetCodeHash = _hasher.HashPassword(user, code);
                user.PasswordResetExpiresUtc = DateTime.UtcNow.AddMinutes(ResetCodeMinutes);
                user.PasswordResetAttempts = 0;
                await _db.SaveChangesAsync(ct);

                var body =
                    $"Your Dhaqaleyste password reset code is: {code}\n" +
                    $"It expires in {ResetCodeMinutes} minutes. If you did not ask for it, ignore this email.\n\n" +
                    $"Koodhkaaga dib-u-dejinta furaha Dhaqaleyste waa: {code}\n" +
                    $"Wuxuu dhacayaa {ResetCodeMinutes} daqiiqo kadib. Haddii aadan codsan, iska indha tir.";
                if (await _email.SendAsync(user.Email, "Dhaqaleyste password reset code", body, ct))
                    _logger.LogInformation("Password reset code sent to staff {UserId}", user.UserId);
            }

            return Ok(new { message = "If an account exists for this email, a code has been sent to it." });
        }

        /// <summary>Sets a new password using the emailed code. Five wrong tries cancel the code.</summary>
        [HttpPost("reset-password")]
        [AllowAnonymous]
        [EnableRateLimiting(RateLimitPolicies.Auth)]
        public async Task<IActionResult> ResetPassword(ResetPasswordWithCodeRequest request)
        {
            const string invalid = "The code is wrong or has expired. Ask for a new code.";
            var email = request.Email.Trim().ToLowerInvariant();
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (user == null || !user.IsActive || string.IsNullOrEmpty(user.PasswordResetCodeHash) ||
                user.PasswordResetExpiresUtc == null || user.PasswordResetExpiresUtc < DateTime.UtcNow)
                return ValidationError(nameof(request.Code), invalid);

            var passwordError = PasswordRules.Validate(request.NewPassword);
            if (passwordError != null)
                return ValidationError(nameof(request.NewPassword), passwordError);

            if (_hasher.VerifyHashedPassword(user, user.PasswordResetCodeHash, request.Code.Trim()) == PasswordVerificationResult.Failed)
            {
                user.PasswordResetAttempts++;
                if (user.PasswordResetAttempts >= MaxResetAttempts)
                {
                    user.PasswordResetCodeHash = null;
                    user.PasswordResetExpiresUtc = null;
                    _logger.LogWarning("Password reset code for staff {UserId} cancelled after repeated wrong tries", user.UserId);
                }
                await _db.SaveChangesAsync();
                return ValidationError(nameof(request.Code), invalid);
            }

            user.PasswordHash = _hasher.HashPassword(user, request.NewPassword);
            user.PasswordResetCodeHash = null;
            user.PasswordResetExpiresUtc = null;
            user.PasswordResetAttempts = 0;
            user.AccessFailedCount = 0;
            user.LockoutEndUtc = null;
            user.SecurityStamp = Guid.NewGuid().ToString("N");
            user.UpdatedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            await _tokens.RevokeAllAsync(user.UserId);

            _logger.LogInformation("Staff {UserId} reset their password with an emailed code", user.UserId);
            return NoContent();
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
