namespace Mall.Api.Infrastructure;

public sealed class DatabaseOptions
{
    public const string SectionName = "Database";
    public string ConnectionString { get; init; } = string.Empty;
}

public sealed class RedisOptions
{
    public const string SectionName = "Redis";
    public string ConnectionString { get; init; } = "localhost:6379";
}

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";
    public string Issuer { get; init; } = "easy-shop";
    public string Audience { get; init; } = "easy-shop-client";
    public string SigningKey { get; init; } = string.Empty;
    public int AccessTokenMinutes { get; init; } = 720;
}

public sealed class WeChatOptions
{
    public const string SectionName = "WeChat";
    public string AppId { get; init; } = string.Empty;
    public string AppSecret { get; init; } = string.Empty;
    public string SessionEndpoint { get; init; } = "https://api.weixin.qq.com/sns/jscode2session";
}

public sealed class PaymentOptions
{
    public const string SectionName = "Payment";
    public string Mode { get; init; } = "mock";
    public string CallbackSecret { get; init; } = "development-payment-secret-change-me";
}
