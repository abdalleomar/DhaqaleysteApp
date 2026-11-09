using DhaqaleysteApi.Data;
using DhaqaleysteApi.Dtos;
using DhaqaleysteApi.Models;
using DhaqaleysteApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DhaqaleysteApi.Controllers
{
    /// <summary>Staff account management. Admins only. Accounts are deactivated, never deleted.</summary>
    [Route("api/staff")]
    [Authorize(Policy = AuthPolicies.Admin)]
    public class StaffController : ApiControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IPasswordHasher<User> _hasher;
        private readonly TokenService _tokens;
        private readonly ILogger<StaffController> _logger;

        public StaffController(AppDbContext db, IPasswordHasher<User> hasher, TokenService tokens, ILogger<StaffController> logger)
        {
            _db = db;
            _hasher = hasher;
            _tokens = tokens;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<StaffDto>>> GetAll()
        {
            var users = await _db.Users.AsNoTracking().OrderBy(x => x.FullName).ToListAsync();
            return users.Select(StaffDto.From).ToList();
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<StaffDto>> Get(int id)
        {
            var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == id);
            return user == null ? NotFound() : StaffDto.From(user);
        }

        [HttpPost]
        public async Task<ActionResult<StaffDto>> Create(CreateStaffRequest request)
        {
            if (!Roles.IsAssignable(request.Role))
                return ValidationError(nameof(request.Role), "Role must be Admin or Staff.");

            var passwordError = PasswordRules.Validate(request.Password);
            if (passwordError != null)
                return ValidationError(nameof(request.Password), passwordError);

            var email = NormalizeEmail(request.Email);
            var phone = request.Phone?.Trim() ?? "";
            var conflict = await FindConflictAsync(email, phone, excludeId: null);
            if (conflict != null)
                return conflict;

            var user = new User
            {
                FullName = request.FullName.Trim(),
                Email = email,
                Phone = phone,
                Role = request.Role,
                IsActive = true
            };
            user.PasswordHash = _hasher.HashPassword(user, request.Password);

            _db.Users.Add(user);
            await _db.SaveChangesAsync();
            _logger.LogInformation("Admin {AdminId} created staff {UserId} ({Role})", CurrentUserId, user.UserId, user.Role);

            return CreatedAtAction(nameof(Get), new { id = user.UserId }, StaffDto.From(user));
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<StaffDto>> Update(int id, UpdateStaffRequest request)
        {
            var user = await _db.Users.FirstOrDefaultAsync(x => x.UserId == id);
            if (user == null)
                return NotFound();

            if (!Roles.IsAssignable(request.Role))
                return ValidationError(nameof(request.Role), "Role must be Admin or Staff.");

            if (id == CurrentUserId && (!request.IsActive || request.Role != Roles.Admin))
                return ValidationError(nameof(request.Role), "You cannot deactivate or demote your own account.");

            if (request.IsActive && string.IsNullOrEmpty(user.PasswordHash))
                return ValidationError(nameof(request.IsActive), "Set a password for this account before activating it.");

            var email = NormalizeEmail(request.Email);
            var phone = request.Phone?.Trim() ?? "";
            var conflict = await FindConflictAsync(email, phone, excludeId: id);
            if (conflict != null)
                return conflict;

            var permissionsChanged = user.Role != request.Role || user.IsActive != request.IsActive;

            user.FullName = request.FullName.Trim();
            user.Email = email;
            user.Phone = phone;
            user.Role = request.Role;
            user.IsActive = request.IsActive;
            user.UpdatedAtUtc = DateTime.UtcNow;

            if (permissionsChanged)
                user.SecurityStamp = Guid.NewGuid().ToString("N");

            await _db.SaveChangesAsync();

            if (permissionsChanged)
            {
                await _tokens.RevokeAllAsync(user.UserId);
                _logger.LogInformation("Admin {AdminId} changed staff {UserId}: role {Role}, active {Active}",
                    CurrentUserId, user.UserId, user.Role, user.IsActive);
            }

            return StaffDto.From(user);
        }

        [HttpPost("{id:int}/reset-password")]
        public async Task<IActionResult> ResetPassword(int id, ResetPasswordRequest request)
        {
            var user = await _db.Users.FirstOrDefaultAsync(x => x.UserId == id);
            if (user == null)
                return NotFound();

            var error = PasswordRules.Validate(request.NewPassword);
            if (error != null)
                return ValidationError(nameof(request.NewPassword), error);

            user.PasswordHash = _hasher.HashPassword(user, request.NewPassword);
            user.SecurityStamp = Guid.NewGuid().ToString("N");
            user.AccessFailedCount = 0;
            user.LockoutEndUtc = null;
            user.UpdatedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            await _tokens.RevokeAllAsync(user.UserId);

            _logger.LogInformation("Admin {AdminId} reset the password of staff {UserId}", CurrentUserId, user.UserId);
            return NoContent();
        }

        private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

        private async Task<ActionResult?> FindConflictAsync(string email, string phone, int? excludeId)
        {
            if (await _db.Users.AnyAsync(x => x.Email == email && x.UserId != excludeId))
                return ValidationError("Email", "Another account already uses this email.");

            if (phone.Length > 0 && await _db.Users.AnyAsync(x => x.Phone == phone && x.UserId != excludeId))
                return ValidationError("Phone", "Another account already uses this phone number.");

            return null;
        }
    }
}
