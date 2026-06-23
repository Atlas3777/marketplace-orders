using FluentAssertions;
using Marketplace.Orders.Application;
using Marketplace.Orders.Application.DTOs;
using Marketplace.Orders.Application.Implementation;
using Marketplace.Orders.Domain;
using Moq;
using Xunit;

namespace Marketplace.Orders.UnitTests.ApplicationTests;

public class OrderServiceTests
{
    private readonly Mock<IOrderRepository> _orderRepositoryMock;
    private readonly Mock<IProductGrpcClient> _productGrpcClientMock;
    private readonly OrderService _orderService;

    public OrderServiceTests()
    {
        _orderRepositoryMock = new Mock<IOrderRepository>();
        _productGrpcClientMock = new Mock<IProductGrpcClient>();
        
        _orderService = new OrderService(
            _orderRepositoryMock.Object, 
            _productGrpcClientMock.Object
        );
    }

    [Fact]
    public async Task CreateOrderAsync_ShouldCreateOrderSuccessfully_WhenProductsExist()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var productId1 = Guid.NewGuid();
        var productId2 = Guid.NewGuid();

        var dto = new CreateOrderDto(userId, new List<CreateOrderItemDto>
        {
            new(productId1, 2),
            new(productId2, 1)
        });

        _productGrpcClientMock
            .Setup(x => x.GetProductByIdAsync(productId1))
            .ReturnsAsync(new ProductInfo(productId1, 150.00m));

        _productGrpcClientMock
            .Setup(x => x.GetProductByIdAsync(productId2))
            .ReturnsAsync(new ProductInfo(productId2, 200.00m));

        // Act
        var orderId = await _orderService.CreateOrderAsync(dto);

        // Assert
        orderId.Should().NotBeEmpty();

        _orderRepositoryMock.Verify(repo => repo.AddAsync(It.Is<Order>(order =>
            order.Id == orderId &&
            order.UserId == userId &&
            order.Status == OrderStatus.Created &&
            order.TotalPrice == 500.00m && 
            order.Items.Count == 2 &&
            order.Items[0].ProductId == productId1 &&
            order.Items[0].Price == 150.00m &&
            order.Items[1].ProductId == productId2 &&
            order.Items[1].Price == 200.00m
        )), Times.Once);
    }

    [Fact]
    public async Task CreateOrderAsync_ShouldThrowKeyNotFoundException_WhenProductDoesNotExist()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var dto = new CreateOrderDto(Guid.NewGuid(), new List<CreateOrderItemDto>
        {
            new(productId, 1)
        });

        _productGrpcClientMock
            .Setup(x => x.GetProductByIdAsync(productId))
            .ReturnsAsync((ProductInfo?)null);

        // Act
        Func<Task> act = async () => await _orderService.CreateOrderAsync(dto);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"Товар {productId} не найден!");

        _orderRepositoryMock.Verify(repo => repo.AddAsync(It.IsAny<Order>()), Times.Never);
    }

    [Theory]
    [InlineData(-1, 5, 0, 5)]    // Индекс страницы меньше нуля сбрасывается в 0
    [InlineData(2, 0, 20, 10)]   // Размер страницы <= 0 сбрасывается в 10
    [InlineData(2, -5, 20, 10)]  // Размер страницы <= 0 сбрасывается в 10
    [InlineData(3, 20, 60, 20)]  // Корректный расчет: offset = 3 * 20 = 60, limit = 20
    public async Task GetOrdersPagedAsync_ShouldAdjustPaginationParametersCorrectly(
        int pageIndex, int pageSize, int expectedOffset, int expectedLimit)
    {
        // Arrange
        var userId = Guid.NewGuid();

        _orderRepositoryMock
            .Setup(repo => repo.GetPagedAsync(userId, It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new List<Order>());

        // Act
        await _orderService.GetOrdersPagedAsync(userId, pageIndex, pageSize);

        // Assert
        _orderRepositoryMock.Verify(repo => repo.GetPagedAsync(userId, expectedOffset, expectedLimit), Times.Once);
    }

    [Fact]
    public async Task UpdateOrderStatusAsync_ShouldCallRepositoryWithCorrectParameters()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var newStatus = OrderStatus.Shipped;

        // Act
        await _orderService.UpdateOrderStatusAsync(orderId, newStatus);

        // Assert
        _orderRepositoryMock.Verify(repo => repo.UpdateStatusAsync(orderId, newStatus), Times.Once);
    }
    
    [Fact]
    public async Task GetOrderAsync_ShouldReturnOrder_WhenOrderExists()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var expectedOrder = new Order { Id = orderId, UserId = Guid.NewGuid() };

        _orderRepositoryMock
            .Setup(repo => repo.GetByIdAsync(orderId))
            .ReturnsAsync(expectedOrder);

        // Act
        var result = await _orderService.GetOrderAsync(orderId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(orderId);
        _orderRepositoryMock.Verify(repo => repo.GetByIdAsync(orderId), Times.Once);
    }

    [Fact]
    public async Task GetOrderAsync_ShouldReturnNull_WhenOrderDoesNotExist()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        _orderRepositoryMock
            .Setup(repo => repo.GetByIdAsync(orderId))
            .ReturnsAsync((Order?)null);

        // Act
        var result = await _orderService.GetOrderAsync(orderId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateOrderAsync_ShouldCalculateTotalPriceCorrectly_ForMultipleItemsOfSameAndDifferentProducts()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var prodId1 = Guid.NewGuid();
        var prodId2 = Guid.NewGuid();

        var dto = new CreateOrderDto(userId, new List<CreateOrderItemDto>
        {
            new(prodId1, 3), // 3 * 100 = 300
            new(prodId2, 2)  // 2 * 250 = 500. Итого = 800
        });

        _productGrpcClientMock.Setup(x => x.GetProductByIdAsync(prodId1))
            .ReturnsAsync(new ProductInfo(prodId1, 100.00m));
        _productGrpcClientMock.Setup(x => x.GetProductByIdAsync(prodId2))
            .ReturnsAsync(new ProductInfo(prodId2, 250.00m));

        // Act
        var orderId = await _orderService.CreateOrderAsync(dto);

        // Assert
        _orderRepositoryMock.Verify(repo => repo.AddAsync(It.Is<Order>(order =>
            order.TotalPrice == 800.00m &&
            order.Items.Count == 2
        )), Times.Once);
    }
}