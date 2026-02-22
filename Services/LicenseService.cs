using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ECoopSystem.Build;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ECoopSystem.Services;

public class LicenseService 
{
    private readonly HttpClient _http;
    private readonly IConfiguration _config;
    private readonly ILogger<LicenseService> _logger;
    private readonly string _baseUrl;

    public LicenseService(HttpClient http, IConfiguration config, ILogger<LicenseService> logger)
    {
        _http = http;
        _config = config;
        _logger = logger;
        _http.MaxResponseContentBufferSize = 1024 * 1024;

        // Always use BuildConfiguration.ApiUrl (compiled at build time)
        _baseUrl = BuildConfiguration.ApiUrl;
        _logger.LogInformation("LicenseService initialized with API URL: {BaseUrl}", _baseUrl);
    }

    public async Task<ActivateResult> ActivateAsync(string licenseKey, string fingerprint, CancellationToken ct)
    {
        const string encEndpoint = "L3dlYi9hcGkvdjEvbGljZW5zZS9hY3RpdmF0ZQ==";
        var endpoint = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(encEndpoint));
        var url = $"{_baseUrl.TrimEnd('/')}{endpoint}";
        
        _logger.LogInformation("Activation URL: {Url}", url);
  
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

        using var resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);

        _logger.LogDebug("Activation request completed with status: {StatusCode}", resp.StatusCode);
        
        if (resp.StatusCode == HttpStatusCode.OK)
        {
            var secret = await resp.Content.ReadFromJsonAsync<string>(cancellationToken: ct);
            secret = secret?.Trim();

            if (string.IsNullOrWhiteSpace(secret))
            {
                _logger.LogWarning("Activation succeeded but secret key is empty");
                return ActivateResult.ServerError(200, "Activation succeeded but secret key is empty.");
            }

            _logger.LogInformation("License activation successful");
            return ActivateResult.Success(secret);
        }

        if (resp.StatusCode == HttpStatusCode.BadRequest)
        {
            var err = await TryReadErrorAsync(resp, ct);
            _logger.LogWarning("Invalid license key provided: {Error}", err);
            return ActivateResult.InvalidKey(err ?? "Activation failed");
        }

        var status = (int)resp.StatusCode;
        var msg = await TryReadErrorAsync(resp, ct);
        _logger.LogError("Activation server error: Status {Status}, Message: {Message}", status, msg);
        return ActivateResult.ServerError(status, msg);
    }

    public async Task<VerifyResult> VerifyAsync(string secretKey, string fingerprint, int counter, CancellationToken ct)
    {
        const string encEndpoint = "L3dlYi9hcGkvdjEvbGljZW5zZS92ZXJpZnk=";
        var endpoint = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(encEndpoint));
        var url = $"{ApiService.BaseUrl.TrimEnd('/')}{endpoint}";

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

        using var resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);

        _logger.LogDebug("Verification request completed with status: {StatusCode}", resp.StatusCode);
        
        if (resp.StatusCode == HttpStatusCode.OK || resp.StatusCode == HttpStatusCode.NoContent)
        {
            _logger.LogInformation("License verification successful");
            return VerifyResult.Ok();
        }
            


        if (resp.StatusCode == HttpStatusCode.NotFound)
        {
            var err = await TryReadErrorAsync(resp, ct);
            _logger.LogWarning("License not found or invalid: {Error}", err);
            return VerifyResult.Invalid(err ?? "License not found or invalid");
        }

        var status = (int)resp.StatusCode;
        var msg = await TryReadErrorAsync(resp, ct);
        _logger.LogError("Verification server error: Status {Status}, Message: {Message}", status, msg);

        return VerifyResult.ServerError(status, msg);
    }

    private static async Task<string?> TryReadErrorAsync(HttpResponseMessage resp, CancellationToken ct)
    {
        try
        {
            var apiErr = await resp.Content.ReadFromJsonAsync<ApiError>(cancellationToken: ct);
            return apiErr?.error;
        }
        catch
        {
            try
            {
                var text = await resp.Content.ReadAsStringAsync(ct);
                return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
            }
            catch
            {
                return null;
            }
        }
    }

    private sealed class ActivateRequest
    {
        public string license_key { get; set; } = "";
        public string fingerprint { get; set; } = "";
    }

    private sealed class VerifyRequest
    {
        public string secret_key { get; set; } = "";
        public string fingerprint { get; set; } = "";
        public int counter { get; set; }
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
