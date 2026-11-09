using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using DhaqaleysteApp.Models;

namespace DhaqaleysteApp.Services;

/// <summary>
/// Adds the staff access token to API requests and, when it has expired, refreshes it once
/// with the refresh token and retries the request.
/// </summary>
public class AuthHandler : DelegatingHandler
{
    private static readonly SemaphoreSlim RefreshLock = new(1, 1);

    private readonly SessionService _session;
    private readonly AppConfig _config;

    public AuthHandler(SessionService session, AppConfig config)
    {
        _session = session;
        _config = config;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var token = await _session.GetAccessTokenAsync();
        if (!string.IsNullOrEmpty(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await base.SendAsync(request, ct);
        if (response.StatusCode != HttpStatusCode.Unauthorized || string.IsNullOrEmpty(token) || IsAuthEndpoint(request))
            return response;

        var newToken = await RefreshAsync(token, ct);
        if (newToken == null)
            return response;

        // Content streams can only be sent once; uploads are re-sent by the caller instead.
        if (request.Content is StreamContent or MultipartFormDataContent)
            return response;

        response.Dispose();
        var retry = await CloneAsync(request);
        retry.Headers.Authorization = new AuthenticationHeaderValue("Bearer", newToken);
        return await base.SendAsync(retry, ct);
    }

    private async Task<string?> RefreshAsync(string expiredToken, CancellationToken ct)
    {
        await RefreshLock.WaitAsync(ct);
        try
        {
            // Another request may already have refreshed while we waited.
            var current = await _session.GetAccessTokenAsync();
            if (!string.IsNullOrEmpty(current) && current != expiredToken)
                return current;

            var refreshToken = await _session.GetRefreshTokenAsync();
            if (string.IsNullOrEmpty(refreshToken) || _config.ApiBaseUrl == null)
                return null;

            var request = new HttpRequestMessage(HttpMethod.Post, new Uri(_config.ApiBaseUrl, "api/auth/refresh"))
            {
                Content = JsonContent.Create(new { refreshToken })
            };
            using var response = await base.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == HttpStatusCode.Unauthorized)
                    _session.Clear();
                return null;
            }

            var auth = await response.Content.ReadFromJsonAsync<AuthResponse>(cancellationToken: ct);
            if (auth == null)
                return null;

            await _session.SaveAsync(auth);
            return auth.AccessToken;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return null;
        }
        finally
        {
            RefreshLock.Release();
        }
    }

    private static bool IsAuthEndpoint(HttpRequestMessage request) =>
        request.RequestUri?.AbsolutePath.Contains("/api/auth/", StringComparison.OrdinalIgnoreCase) == true;

    private static async Task<HttpRequestMessage> CloneAsync(HttpRequestMessage request)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri);
        if (request.Content != null)
        {
            var bytes = await request.Content.ReadAsByteArrayAsync();
            clone.Content = new ByteArrayContent(bytes);
            foreach (var header in request.Content.Headers)
                clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }
        foreach (var header in request.Headers)
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        return clone;
    }
}
