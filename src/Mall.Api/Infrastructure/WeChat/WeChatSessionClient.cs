using System.Text.Json;
using Mall.Api.Infrastructure;
using Microsoft.Extensions.Options;

namespace Mall.Api.Infrastructure.WeChat;

public interface IWeChatSessionClient
{
    Task<WeChatSessionResult> ExchangeCodeAsync(string code, CancellationToken cancellationToken);
}

public sealed class WeChatSessionClient(HttpClient httpClient, IOptions<WeChatOptions> options) : IWeChatSessionClient
{
    private readonly WeChatOptions _options = options.Value;

    public async Task<WeChatSessionResult> ExchangeCodeAsync(string code, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.AppId) || string.IsNullOrWhiteSpace(_options.AppSecret))
            throw new InvalidOperationException("微信小程序配置未完成。");
        var url = $"{_options.SessionEndpoint}?appid={Uri.EscapeDataString(_options.AppId)}&secret={Uri.EscapeDataString(_options.AppSecret)}&js_code={Uri.EscapeDataString(code)}&grant_type=authorization_code";
        using var response = await httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<WeChatSessionResult>(Common.AppJsonSerializerContext.Default.WeChatSessionResult, cancellationToken);
        return result ?? throw new InvalidOperationException("微信登录响应为空。");
    }
}

public sealed record WeChatSessionResult(string? OpenId, string? UnionId, string? SessionKey, int ErrorCode = 0, string? ErrorMessage = null)
{
    public bool IsSuccess => ErrorCode == 0 && !string.IsNullOrWhiteSpace(OpenId);
}
