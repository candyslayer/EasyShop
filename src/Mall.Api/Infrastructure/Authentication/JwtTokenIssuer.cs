using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Mall.Api.Common;
using Mall.Api.Infrastructure;

namespace Mall.Api.Infrastructure.Authentication;

public sealed class JwtTokenIssuer(IConfiguration configuration)
{
    private readonly JwtOptions _options = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new();

    public string Issue(long userId, string? username, bool admin)
    {
        var now = DateTimeOffset.UtcNow;
        var header = Base64Url(JsonSerializer.SerializeToUtf8Bytes(new JwtTokenHeader("HS256", "JWT"), AppJsonSerializerContext.Default.JwtTokenHeader));
        var payload = Base64Url(JsonSerializer.SerializeToUtf8Bytes(new JwtTokenPayload(
            userId.ToString(), username ?? string.Empty, _options.Issuer, _options.Audience,
            now.ToUnixTimeSeconds(), now.AddMinutes(_options.AccessTokenMinutes).ToUnixTimeSeconds(), admin ? "admin" : "user"), AppJsonSerializerContext.Default.JwtTokenPayload));
        var unsigned = $"{header}.{payload}";
        var key = Encoding.UTF8.GetBytes(string.IsNullOrWhiteSpace(_options.SigningKey)
            ? "development-only-signing-key-change-me" : _options.SigningKey);
        using var hmac = new HMACSHA256(key);
        return $"{unsigned}.{Base64Url(hmac.ComputeHash(Encoding.UTF8.GetBytes(unsigned)))}";
    }

    private static string Base64Url(ReadOnlySpan<byte> bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
