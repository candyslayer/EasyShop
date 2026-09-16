using Mall.Api.Domain.Users;
using Mall.Api.Infrastructure.Authentication;
using Mall.Api.Infrastructure.Persistence;
using Mall.Api.Infrastructure.WeChat;
using Microsoft.EntityFrameworkCore;

namespace Mall.Api.Application.Auth;

public sealed class AuthUseCases(
    MallDbContext db,
    JwtTokenIssuer tokenIssuer,
    IWeChatSessionClient weChatClient)
{
    public async Task<AuthResponse?> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        if (await db.Users.AnyAsync(x => x.Username == request.Username, cancellationToken)) return null;
        var now = DateTime.UtcNow;
        var user = new User
        {
            Username = request.Username.Trim(),
            PasswordHash = PasswordHasher.Hash(request.Password),
            Mobile = request.Mobile,
            Nickname = string.IsNullOrWhiteSpace(request.Nickname) ? request.Username.Trim() : request.Nickname.Trim(),
            CreatedAt = now,
            UpdatedAt = now
        };
        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);
        return CreateResponse(user);
    }

    public async Task<AuthResponse?> LoginAsync(PasswordLoginRequest request, CancellationToken cancellationToken)
    {
        var user = await db.Users.Include(x => x.Roles).ThenInclude(x => x.Role).SingleOrDefaultAsync(x => x.Username == request.Username && x.Enabled, cancellationToken);
        if (user is null || user.PasswordHash is null || !PasswordHasher.Verify(request.Password, user.PasswordHash)) return null;
        return CreateResponse(user);
    }

    public async Task<AuthResponse> WeChatLoginAsync(WeChatLoginRequest request, CancellationToken cancellationToken)
    {
        var session = await weChatClient.ExchangeCodeAsync(request.Code, cancellationToken);
        if (!session.IsSuccess) throw new InvalidOperationException(session.ErrorMessage ?? "微信登录失败。");
        var user = await db.Users.Include(x => x.Roles).ThenInclude(x => x.Role).SingleOrDefaultAsync(x => x.WechatOpenId == session.OpenId, cancellationToken);
        if (user is null)
        {
            var now = DateTime.UtcNow;
            user = new User { WechatOpenId = session.OpenId, WechatUnionId = session.UnionId, Nickname = "微信用户", CreatedAt = now, UpdatedAt = now };
            db.Users.Add(user);
            await db.SaveChangesAsync(cancellationToken);
        }
        return CreateResponse(user);
    }

    private AuthResponse CreateResponse(User user) => new(
        tokenIssuer.Issue(user.Id, user.Username, user.Roles.Any(x => x.Role?.Code == "admin")),
        new UserProfileResponse(user.Id, user.Username, user.Mobile, user.Nickname, user.AvatarUrl));
}
