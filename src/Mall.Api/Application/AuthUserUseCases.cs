using Mall.Api.Domain.Users;
using Mall.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Mall.Api.Application.Auth;

public sealed class UserUseCases(MallDbContext db)
{
    public Task<User?> GetAsync(long userId, CancellationToken cancellationToken) =>
        db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == userId && x.Enabled, cancellationToken);

    public async Task<IReadOnlyList<UserAddress>> GetAddressesAsync(long userId, CancellationToken cancellationToken) =>
        await db.UserAddresses.AsNoTracking().Where(x => x.UserId == userId).OrderByDescending(x => x.IsDefault).ThenByDescending(x => x.Id).ToListAsync(cancellationToken);

    public async Task<UserAddress> AddAddressAsync(long userId, AddressRequest request, CancellationToken cancellationToken)
    {
        if (request.IsDefault) await ClearDefaultAsync(userId, cancellationToken);
        var now = DateTime.UtcNow;
        var address = new UserAddress { UserId = userId, Consignee = request.Consignee, Mobile = request.Mobile, Province = request.Province, City = request.City, District = request.District, Detail = request.Detail, IsDefault = request.IsDefault, CreatedAt = now, UpdatedAt = now };
        db.UserAddresses.Add(address);
        await db.SaveChangesAsync(cancellationToken);
        return address;
    }

    public async Task<UserAddress?> UpdateAddressAsync(long userId, long id, AddressRequest request, CancellationToken cancellationToken)
    {
        var address = await db.UserAddresses.SingleOrDefaultAsync(x => x.Id == id && x.UserId == userId, cancellationToken);
        if (address is null) return null;
        if (request.IsDefault) await ClearDefaultAsync(userId, cancellationToken);
        address.Consignee = request.Consignee; address.Mobile = request.Mobile; address.Province = request.Province; address.City = request.City; address.District = request.District; address.Detail = request.Detail; address.IsDefault = request.IsDefault; address.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return address;
    }

    public async Task<bool> DeleteAddressAsync(long userId, long id, CancellationToken cancellationToken)
    {
        var address = await db.UserAddresses.SingleOrDefaultAsync(x => x.Id == id && x.UserId == userId, cancellationToken);
        if (address is null) return false;
        db.UserAddresses.Remove(address);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task ClearDefaultAsync(long userId, CancellationToken cancellationToken) =>
        await db.UserAddresses.Where(x => x.UserId == userId && x.IsDefault).ExecuteUpdateAsync(x => x.SetProperty(a => a.IsDefault, false), cancellationToken);
}
