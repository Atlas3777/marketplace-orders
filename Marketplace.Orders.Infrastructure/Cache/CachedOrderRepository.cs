using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Marketplace.Orders.Application;
using Marketplace.Orders.Domain;

namespace Marketplace.Orders.Infrastructure.Cache;

public class CachedOrderRepository : IOrderRepository
{
    private readonly IOrderRepository _inner;
    private readonly IDistributedCache _cache;
    
    private readonly DistributedCacheEntryOptions _cacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
    };

    public CachedOrderRepository(IOrderRepository inner, IDistributedCache cache)
    {
        _inner = inner;
        _cache = cache;
    }

    private static string GetOrderKey(Guid id) => $"order:{id}";
    private static string GetUserVersionKey(Guid userId) => $"user:{userId}:version";

    public async Task AddAsync(Order order)
    {
        await _inner.AddAsync(order);

        await InvalidateUserPagesAsync(order.UserId);

        await _cache.SetStringAsync(GetOrderKey(order.Id), JsonSerializer.Serialize(order), _cacheOptions);
    }

    public async Task<Order?> GetByIdAsync(Guid id)
    {
        var cacheKey = GetOrderKey(id);
        var cachedData = await _cache.GetStringAsync(cacheKey);

        if (!string.IsNullOrEmpty(cachedData))
        {
            return JsonSerializer.Deserialize<Order>(cachedData);
        }

        var order = await _inner.GetByIdAsync(id);

        if (order != null)
        {
            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(order), _cacheOptions);
        }

        return order;
    }

    public async Task<IEnumerable<Order>> GetPagedAsync(Guid userId, int offset, int limit)
    {
        var version = await GetOrCreateUserVersionAsync(userId);
        var cacheKey = $"user:{userId}:v:{version}:page:{offset}:{limit}";

        var cachedData = await _cache.GetStringAsync(cacheKey);
        if (!string.IsNullOrEmpty(cachedData))
        {
            return JsonSerializer.Deserialize<List<Order>>(cachedData) ?? [];
        }

        var orders = (await _inner.GetPagedAsync(userId, offset, limit)).ToList();

        await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(orders), _cacheOptions);

        return orders;
    }

    public async Task UpdateStatusAsync(Guid id, OrderStatus status)
    {
        var order = await _inner.GetByIdAsync(id);
        if (order == null)
        {
            throw new KeyNotFoundException($"Заказ с Id {id} не найден.");
        }

        await _inner.UpdateStatusAsync(id, status);

        await _cache.RemoveAsync(GetOrderKey(id));

        await InvalidateUserPagesAsync(order.UserId);
    }

    private async Task<string> GetOrCreateUserVersionAsync(Guid userId)
    {
        var key = GetUserVersionKey(userId);
        var version = await _cache.GetStringAsync(key);

        if (string.IsNullOrEmpty(version))
        {
            version = Guid.NewGuid().ToString("N"); 
            await _cache.SetStringAsync(key, version, _cacheOptions);
        }

        return version;
    }

    private async Task InvalidateUserPagesAsync(Guid userId)
    {
        var key = GetUserVersionKey(userId);
        await _cache.RemoveAsync(key); 
    }
}