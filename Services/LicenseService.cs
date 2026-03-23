using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ECoopSystem.Build;
using Microsoft.Extensions.Configuration;

namespace ECoopSystem.Services;

public class LicenseService 
{
    private readonly HttpClient _http;
    private readonly string _baseUrl;
    private readonly bool _localEnabled;
    private readonly bool _fallbackToLocalOnServerError;
    private readonly Dictionary<string, string> _localLicenseToSecret = new(StringComparer.Ordinal);
    private readonly HashSet<string> _localSecretKeys = new(StringComparer.Ordinal);
    private static readonly string ActivateEndpoint = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String("L3dlYi9hcGkvdjEvbGljZW5zZS9hY3RpdmF0ZQ=="));
    private static readonly string VerifyEndpoint = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String("L3dlYi9hcGkvdjEvbGljZW5zZS92ZXJpZnk="));

    public LicenseService(HttpClient http, IConfiguration config)
    {
        _http = http;
        _http.MaxResponseContentBufferSize = 1024 * 1024;

        _baseUrl = BuildConfiguration.ApiUrl;

        var localEnabledValue = config["LicenseLocal:Enabled"];
        _localEnabled = bool.TryParse(localEnabledValue, out var enabled) && enabled;

        var fallbackValue = config["LicenseLocal:FallbackToLocalOnServerError"];
        _fallbackToLocalOnServerError = !bool.TryParse(fallbackValue, out var fallback) || fallback;

        var localTestLicense = config["LicenseLocal:TestLicenseKey"]?.Trim();
        if (!string.IsNullOrWhiteSpace(localTestLicense))
        {
            var secret = CreateLocalSecret(localTestLicense);
            _localLicenseToSecret[localTestLicense] = secret;
            _localSecretKeys.Add(secret);
        }
    }

    public async Task<ActivateResult> ActivateAsync(string licenseKey, string fingerprint, CancellationToken ct)
    {
        if (_localEnabled)
        {
            return ActivateLocal(licenseKey);
        }

        try
        {
            var remoteResult = await ActivateRemoteAsync(licenseKey, fingerprint, ct).ConfigureAwait(false);
            if (!_fallbackToLocalOnServerError || remoteResult.IsSuccess || remoteResult.IsInvalidKey)
            {
                return remoteResult;
            }

            var localResult = ActivateLocal(licenseKey);
            return localResult.IsSuccess ? localResult : remoteResult;
        }
        catch (HttpRequestException)
        {
            if (_fallbackToLocalOnServerError)
            {
                return ActivateLocal(licenseKey);
            }

            throw;
        }
        catch (TaskCanceledException)
        {
            if (_fallbackToLocalOnServerError)
            {
                return ActivateLocal(licenseKey);
            }

            throw;
        }
    }

    public async Task<VerifyResult> VerifyAsync(string secretKey, string fingerprint, int counter, CancellationToken ct)
    {
        if (_localEnabled)
        {
            return VerifyLocal(secretKey);
        }

        try
        {
            var remoteResult = await VerifyRemoteAsync(secretKey, fingerprint, counter, ct).ConfigureAwait(false);
            if (!_fallbackToLocalOnServerError || remoteResult.IsOk || remoteResult.IsInvalid)
            {
                return remoteResult;
            }

            var localResult = VerifyLocal(secretKey);
            return localResult.IsOk ? localResult : remoteResult;
        }
        catch (HttpRequestException)
        {
            if (_fallbackToLocalOnServerError)
            {
                return VerifyLocal(secretKey);
            }

            throw;
        }
        catch (TaskCanceledException)
        {
            if (_fallbackToLocalOnServerError)
            {
                return VerifyLocal(secretKey);
            }

            throw;
        }
    }

    private async Task<ActivateResult> ActivateRemoteAsync(string licenseKey, string fingerprint, CancellationToken ct)
    {
        var url = $"{_baseUrl.TrimEnd('/')}{ActivateEndpoint}";

        var payload = new
        {
            license_key = licenseKey,
            fingerprint = fingerprint
        };

        var json = JsonSerializer.Serialize(payload);
        var bytes = Encoding.UTF8.GetBytes(json);

        using var content = new ByteArrayContent(bytes);
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
        content.Headers.ContentLength = bytes.Length;

        using var req = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Version = HttpVersion.Version11,
            VersionPolicy = HttpVersionPolicy.RequestVersionExact,
            Content = content
        };

        req.Headers.TransferEncodingChunked = false;

        using var resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);

        if (resp.StatusCode == HttpStatusCode.OK)
        {
            var secret = await resp.Content.ReadFromJsonAsync<string>(cancellationToken: ct).ConfigureAwait(false);
            secret = secret?.Trim();

            if (string.IsNullOrWhiteSpace(secret))
            {
                return ActivateResult.ServerError(200, "Activation succeeded but secret key is empty.");
            }

            return ActivateResult.Success(secret);
        }

        if (resp.StatusCode == HttpStatusCode.BadRequest)
        {
            var err = await TryReadErrorAsync(resp, ct).ConfigureAwait(false);
            return ActivateResult.InvalidKey(err ?? "Activation failed");
        }

        var status = (int)resp.StatusCode;
        var msg = await TryReadErrorAsync(resp, ct).ConfigureAwait(false);
        return ActivateResult.ServerError(status, msg);
    }

    private async Task<VerifyResult> VerifyRemoteAsync(string secretKey, string fingerprint, int counter, CancellationToken ct)
    {
        var url = $"{_baseUrl.TrimEnd('/')}{VerifyEndpoint}";

        var payload = new
        {
            secret_key = secretKey,
            fingerprint = fingerprint,
            counter = counter
        };

        var json = JsonSerializer.Serialize(payload);
        var bytes = Encoding.UTF8.GetBytes(json);

        using var content = new ByteArrayContent(bytes);
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
        content.Headers.ContentLength = bytes.Length;

        var req = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Version = HttpVersion.Version11,
            VersionPolicy = HttpVersionPolicy.RequestVersionExact,
            Content = content
        };

        req.Headers.TransferEncodingChunked = false;

        using var resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
        
        if (resp.StatusCode == HttpStatusCode.OK || resp.StatusCode == HttpStatusCode.NoContent)
        {
            return VerifyResult.Ok();
        }

        if (resp.StatusCode == HttpStatusCode.NotFound)
        {
            var err = await TryReadErrorAsync(resp, ct).ConfigureAwait(false);
            return VerifyResult.Invalid(err ?? "License not found or invalid");
        }

        var status = (int)resp.StatusCode;
        var msg = await TryReadErrorAsync(resp, ct).ConfigureAwait(false);

        return VerifyResult.ServerError(status, msg);
    }

    private ActivateResult ActivateLocal(string licenseKey)
    {
        if (!_localLicenseToSecret.TryGetValue(licenseKey.Trim(), out var secret))
        {
            return ActivateResult.InvalidKey("Invalid local test license key");
        }

        return ActivateResult.Success(secret);
    }

    private VerifyResult VerifyLocal(string secretKey)
    {
        return _localSecretKeys.Contains(secretKey.Trim())
            ? VerifyResult.Ok()
            : VerifyResult.Invalid("Invalid local test secret key");
    }

    private static string CreateLocalSecret(string licenseKey)
    {
        var bytes = Encoding.UTF8.GetBytes($"local::{licenseKey}");
        return Convert.ToHexString(SHA256.HashData(bytes));
    }

    private static async Task<string?> TryReadErrorAsync(HttpResponseMessage resp, CancellationToken ct)
    {
        try
        {
            var apiErr = await resp.Content.ReadFromJsonAsync<ApiError>(cancellationToken: ct).ConfigureAwait(false);
            return apiErr?.error;
        }
        catch
        {
            try
            {
                var text = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
            }
            catch
            {
                return null;
            }
        }
    }

    private sealed class ApiError
    {
        public string? error { get; set; }
    }
}

public sealed record ActivateResult(bool IsSuccess, bool IsInvalidKey, string? SecretKey, int? StatusCode, string? ErrorMessage)
{
    public static ActivateResult Success(string secret) => new(true, false, secret, 200, null);
    public static ActivateResult InvalidKey(string msg) => new(false, true, null, 400, msg);
    public static ActivateResult ServerError(int status, string? msg) => new(false, false, null, status, msg);
}

public sealed record VerifyResult(bool IsOk, bool IsInvalid, int? StatusCode, string? ErrorMessage)
{
    internal static VerifyResult Ok() => new(true, false, 200, null);
    internal static VerifyResult Invalid(string msg) => new(false, true, 404, msg);
    internal static VerifyResult ServerError(int status, string? msg) => new(false, false, status, msg);
}
