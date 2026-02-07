using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ECoopSystem.Services;

public class LicenseService 
{
    private readonly HttpClient _http;

    public LicenseService(HttpClient http)
    {
        _http = http;
        _http.Timeout = TimeSpan.FromSeconds(12);
    }

    public async Task<ActivateResult> ActivateAsync(string licenseKey, string fingerprint, CancellationToken ct)
    {
        var url = $"{ApiService.BaseUrl.TrimEnd('/')}/web/api/v1/license/activate";
  
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

        Debug.WriteLine("Resp: " + resp);
        if (resp.StatusCode == HttpStatusCode.OK)
        {
            var secret = await resp.Content.ReadFromJsonAsync<string>(cancellationToken: ct);
            secret = secret?.Trim();

            if (string.IsNullOrWhiteSpace(secret))
                return ActivateResult.ServerError(200, "Activation suceeded but secret key is empty.");

            return ActivateResult.Success(secret);
        }

        if (resp.StatusCode == HttpStatusCode.BadRequest)
        {
            var err = await TryReadErrorAsync(resp, ct);
            return ActivateResult.InvalidKey(err ?? "Activation faield");
        }

        var status = (int)resp.StatusCode;
        var msg = await TryReadErrorAsync(resp, ct);
        return ActivateResult.ServerError(status, msg);
    }

    public async Task<VerifyResult> VerifyAsync(string secretKey, string fingerprint, CancellationToken ct)
    {
        var url = $"{ApiService.BaseUrl.TrimEnd('/')}/web/api/v1/license/verify";

        var payload = new
        {
            secret_key = secretKey,
            fingerprint = fingerprint
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

        if (resp.StatusCode == HttpStatusCode.OK || resp.StatusCode == HttpStatusCode.NoContent)
        {
            return VerifyResult.Ok();
        }
            

        if (resp.StatusCode == HttpStatusCode.NotFound)
        {
            var err = await TryReadErrorAsync(resp, ct);
            return VerifyResult.Invalid(err ?? "License not found or invalid");
        }

        var status = (int)resp.StatusCode;
        var msg = await TryReadErrorAsync(resp, ct);

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
