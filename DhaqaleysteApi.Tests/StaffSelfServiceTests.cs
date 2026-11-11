using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using DhaqaleysteApi.Dtos;
using DhaqaleysteApi.Models;

namespace DhaqaleysteApi.Tests;

public class StaffSelfServiceTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public StaffSelfServiceTests(ApiFactory factory) => _factory = factory;

    private HttpClient Anonymous() => _factory.CreateClient();

    private async Task<HttpClient> AdminAsync()
    {
        var client = _factory.CreateClient();
        var auth = await client.LoginAsync(ApiFactory.AdminEmail, ApiFactory.AdminPassword);
        return client.WithToken(auth.AccessToken);
    }

    private async Task<string> NewInviteCodeAsync()
    {
        var admin = await AdminAsync();
        var response = await admin.PostAsync("/api/staff/invite-code", null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<InviteCodeDto>())!.Code;
    }

    private static object Registration(string inviteCode, string? email = null) => new
    {
        fullName = "New Staff",
        email = email ?? $"new-{Guid.NewGuid():N}@dhaqaleyste.test",
        password = "NewStaff123",
        inviteCode
    };

    // ---------- Sign-up with invite code ----------

    [Fact]
    public async Task Staff_can_create_their_own_account_with_the_invite_code()
    {
        var code = await NewInviteCodeAsync();
        Assert.Matches("^[A-Z2-9]{4}-[A-Z2-9]{4}$", code);

        // Lower case and without the dash still works.
        var response = await Anonymous().PostAsJsonAsync("/api/auth/register", Registration(code.Replace("-", "").ToLowerInvariant()));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var auth = (await response.Content.ReadFromJsonAsync<AuthResponse>())!;
        Assert.Equal(Roles.Staff, auth.User.Role);

        // The new account can work with products straight away.
        var staff = Anonymous().WithToken(auth.AccessToken);
        var product = await staff.PostAsJsonAsync("/api/products", TestHelpers.ValidProduct("Added by new staff"));
        Assert.Equal(HttpStatusCode.Created, product.StatusCode);
    }

    [Fact]
    public async Task Sign_up_needs_the_current_invite_code()
    {
        var oldCode = await NewInviteCodeAsync();
        var newCode = await NewInviteCodeAsync();
        Assert.NotEqual(oldCode, newCode);

        Assert.Equal(HttpStatusCode.BadRequest, (await Anonymous().PostAsJsonAsync("/api/auth/register", Registration(oldCode))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Anonymous().PostAsJsonAsync("/api/auth/register", Registration("WRONG-CODE"))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Anonymous().PostAsJsonAsync("/api/auth/register", Registration(newCode))).StatusCode);
    }

    [Fact]
    public async Task Admin_can_turn_sign_up_off()
    {
        var code = await NewInviteCodeAsync();
        var admin = await AdminAsync();
        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync("/api/staff/invite-code")).StatusCode);

        Assert.Equal(HttpStatusCode.BadRequest, (await Anonymous().PostAsJsonAsync("/api/auth/register", Registration(code))).StatusCode);
        Assert.False((await admin.GetFromJsonAsync<InviteCodeDto>("/api/staff/invite-code"))!.Enabled);
    }

    [Fact]
    public async Task Sign_up_cannot_choose_admin_role_or_reuse_an_email()
    {
        var code = await NewInviteCodeAsync();
        var email = $"dup-{Guid.NewGuid():N}@dhaqaleyste.test";
        var first = await Anonymous().PostAsJsonAsync("/api/auth/register",
            new { fullName = "Sneaky", email, password = "Sneaky12345", inviteCode = code, role = "Admin" });
        Assert.Equal(Roles.Staff, (await first.Content.ReadFromJsonAsync<AuthResponse>())!.User.Role);

        var second = await Anonymous().PostAsJsonAsync("/api/auth/register", Registration(code, email));
        Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
    }

    [Fact]
    public async Task Invite_code_is_admin_only_and_never_public()
    {
        await NewInviteCodeAsync();
        Assert.Equal(HttpStatusCode.Unauthorized, (await Anonymous().GetAsync("/api/staff/invite-code")).StatusCode);

        var company = await Anonymous().GetStringAsync("/api/company");
        Assert.DoesNotContain("invite", company, StringComparison.OrdinalIgnoreCase);
    }

    // ---------- Forgot password ----------

    private async Task<(string Email, string Password)> NewStaffAsync()
    {
        var code = await NewInviteCodeAsync();
        var email = $"forgot-{Guid.NewGuid():N}@dhaqaleyste.test";
        var response = await Anonymous().PostAsJsonAsync("/api/auth/register", Registration(code, email));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (email, "NewStaff123");
    }

    private string LastCodeSentTo(string email)
    {
        var mail = _factory.Emails.SentTo(email).Last();
        return Regex.Match(mail.Body, @"\b\d{6}\b").Value;
    }

    [Fact]
    public async Task Forgot_password_emails_a_code_that_sets_a_new_password()
    {
        var (email, oldPassword) = await NewStaffAsync();

        var forgot = await Anonymous().PostAsJsonAsync("/api/auth/forgot-password", new { email });
        Assert.Equal(HttpStatusCode.OK, forgot.StatusCode);
        var code = LastCodeSentTo(email);
        Assert.Equal(6, code.Length);

        var reset = await Anonymous().PostAsJsonAsync("/api/auth/reset-password", new { email, code, newPassword = "BrandNew123" });
        Assert.Equal(HttpStatusCode.NoContent, reset.StatusCode);

        Assert.Equal(HttpStatusCode.Unauthorized,
            (await Anonymous().PostAsJsonAsync("/api/auth/login", new { emailOrPhone = email, password = oldPassword })).StatusCode);
        await Anonymous().LoginAsync(email, "BrandNew123");

        // A code works only once.
        var again = await Anonymous().PostAsJsonAsync("/api/auth/reset-password", new { email, code, newPassword = "Another123" });
        Assert.Equal(HttpStatusCode.BadRequest, again.StatusCode);
    }

    [Fact]
    public async Task Forgot_password_does_not_reveal_unknown_emails()
    {
        var response = await Anonymous().PostAsJsonAsync("/api/auth/forgot-password", new { email = "nobody@dhaqaleyste.test" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Empty(_factory.Emails.SentTo("nobody@dhaqaleyste.test"));
    }

    [Fact]
    public async Task Five_wrong_codes_cancel_the_reset()
    {
        var (email, _) = await NewStaffAsync();
        await Anonymous().PostAsJsonAsync("/api/auth/forgot-password", new { email });
        var code = LastCodeSentTo(email);
        var wrong = code == "000000" ? "111111" : "000000";

        for (var i = 0; i < 5; i++)
            await Anonymous().PostAsJsonAsync("/api/auth/reset-password", new { email, code = wrong, newPassword = "Guessing123" });

        var correctTooLate = await Anonymous().PostAsJsonAsync("/api/auth/reset-password", new { email, code, newPassword = "Guessing123" });
        Assert.Equal(HttpStatusCode.BadRequest, correctTooLate.StatusCode);
    }

    // ---------- Admin removes staff ----------

    [Fact]
    public async Task Admin_can_remove_and_restore_staff()
    {
        var (email, password) = await NewStaffAsync();
        var staffClient = Anonymous().WithToken((await Anonymous().LoginAsync(email, password)).AccessToken);

        var admin = await AdminAsync();
        var staff = (await admin.GetFromJsonAsync<List<StaffDto>>("/api/staff"))!.Single(s => s.Email == email);

        var remove = await admin.PutAsJsonAsync($"/api/staff/{staff.UserId}",
            new { fullName = staff.FullName, email = staff.Email, role = Roles.Staff, isActive = false });
        Assert.Equal(HttpStatusCode.OK, remove.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await staffClient.GetAsync("/api/auth/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await Anonymous().PostAsJsonAsync("/api/auth/login", new { emailOrPhone = email, password })).StatusCode);

        // A removed account cannot get a reset code either.
        var before = _factory.Emails.SentTo(email).Count;
        await Anonymous().PostAsJsonAsync("/api/auth/forgot-password", new { email });
        Assert.Equal(before, _factory.Emails.SentTo(email).Count);

        var restore = await admin.PutAsJsonAsync($"/api/staff/{staff.UserId}",
            new { fullName = staff.FullName, email = staff.Email, role = Roles.Staff, isActive = true });
        Assert.Equal(HttpStatusCode.OK, restore.StatusCode);
        await Anonymous().LoginAsync(email, password);
    }
}
