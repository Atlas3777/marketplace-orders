using System.Text.Json;
using FluentAssertions;
using Marketplace.Orders.Application;
using Marketplace.Orders.Domain;
using Marketplace.Orders.Infrastructure.Cache;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Marketplace.Orders.UnitTests.InfrastructureTests;

public class CachedOrderRepositoryTests
{
    private readonly Mock<IOrderRepository> _innerRepositoryMock;
    private readonly IDistributedCache _memoryCache;
    private readonly CachedOrderRepository _cachedRepository;

    public CachedOrderRepositoryTests()
    {
        _innerRepositoryMock = new Mock<IOrderRepository>();
        
        // Используем реальный MemoryDistributedCache вместо Moq
        var opts = Options.Create(new MemoryDistributedCacheOptions());
        _memoryCache = new MemoryDistributedCache(opts);

        _cachedRepository = new CachedOrderRepository(
            _innerRepositoryMock.Object, 
            _memoryCache
        );
    }

    [Fact]
    public async Task GetByIdAsync_ShouldFetchFromInnerRepository_WhenCacheIsEmpty()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var expectedOrder = new Order 
        { 
            Id = orderId, 
            UserId = Guid.NewGuid(), 
            TotalPrice = 1000m 
        };

        _innerRepositoryMock
            .Setup(repo => repo.GetByIdAsync(orderId))
            .ReturnsAsync(expectedOrder);

        // Act
        var result = await _cachedRepository.GetByIdAsync(orderId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(orderId);
        
        // Проверяем, что вызов к БД был
        _innerRepositoryMock.Verify(repo => repo.GetByIdAsync(orderId), Times.Once);

        // Проверяем, что данные осели в кэше
        var cachedData = await _memoryCache.GetStringAsync($"order:{orderId}");
        cachedData.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnFromCache_WhenDataIsCached()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var cachedOrder = new Order 
        { 
            Id = orderId, 
            UserId = Guid.NewGuid(), 
            TotalPrice = 500m 
        };

        // Напрямую забиваем данные в наш фейковый кэш
        await _memoryCache.SetStringAsync($"order:{orderId}", JsonSerializer.Serialize(cachedOrder));

        // Act
        var result = await _cachedRepository.GetByIdAsync(orderId);

        // Assert
        result.Should().NotBeNull();
        result!.TotalPrice.Should().Be(500m);
        
        // К базе обращений быть не должно!
        _innerRepositoryMock.Verify(repo => repo.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task AddAsync_ShouldSaveToInnerRepositoryAndCacheOrder()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var order = new Order 
        { 
            Id = Guid.NewGuid(), 
            UserId = userId, 
            TotalPrice = 300m 
        };

        // Инициализируем версию страниц пользователя в кэше, чтобы проверить сброс
        await _memoryCache.SetStringAsync($"user:{userId}:version", "old-version");

        // Act
        await _cachedRepository.AddAsync(order);

        // Assert
        // 1. Проверяем вызов репозитория базы данных
        _innerRepositoryMock.Verify(repo => repo.AddAsync(order), Times.Once);

        // 2. Проверяем, что сам заказ закэшировался
        var cachedOrderData = await _memoryCache.GetStringAsync($"order:{order.Id}");
        cachedOrderData.Should().NotBeNull();

        // 3. Проверяем инвалидацию страниц (ключ версии должен удалиться)
        var userVersion = await _memoryCache.GetStringAsync($"user:{userId}:version");
        userVersion.Should().BeNull();
    }

    [Fact]
    public async Task UpdateStatusAsync_ShouldEvictCacheAndInvalidateUserPages()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var existingOrder = new Order { Id = orderId, UserId = userId };

        _innerRepositoryMock.Setup(repo => repo.GetByIdAsync(orderId)).ReturnsAsync(existingOrder);
        
        // Заполняем кэш перед апдейтом
        await _memoryCache.SetStringAsync($"order:{orderId}", JsonSerializer.Serialize(existingOrder));
        await _memoryCache.SetStringAsync($"user:{userId}:version", "v1");

        // Act
        await _cachedRepository.UpdateStatusAsync(orderId, OrderStatus.Shipped);

        // Assert
        _innerRepositoryMock.Verify(repo => repo.UpdateStatusAsync(orderId, OrderStatus.Shipped), Times.Once);
        
        // Проверяем, что кэш заказа очищен
        (await _memoryCache.GetStringAsync($"order:{orderId}")).Should().BeNull();
        
        // Проверяем, что версия страниц сброшена
        (await _memoryCache.GetStringAsync($"user:{userId}:version")).Should().BeNull();
    }
}