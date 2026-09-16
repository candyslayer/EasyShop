namespace Mall.Api.Application.Auth;

public sealed record RegisterRequest(
    string Username,
    string Password,
    string? Nickname = null,
    string? Mobile = null);

public sealed record PasswordLoginRequest(
    string Username,
    string Password);

public sealed record WeChatLoginRequest(string Code);

public sealed record AuthResponse(string AccessToken, UserProfileResponse User);
public sealed record UserProfileResponse(long Id, string? Username, string? Mobile, string Nickname, string? AvatarUrl);

public sealed record AddressRequest(
    string Consignee,
    string Mobile,
    string Province,
    string City,
    string District,
    string Detail,
    bool IsDefault = false);
