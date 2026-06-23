using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using FluentValidation;
using FluentValidation.Results;
using Marketplace.Orders.Api.Controllers;
using Marketplace.Orders.Application;
using Marketplace.Orders.Application.DTOs;
using Marketplace.Orders.Domain;

namespace Marketplace.Orders.UnitTests.ApiTests;

public class OrdersControllerTests
{
    private readonly Mock<IOrderService> _orderServiceMock;
    private readonly Mock<IValidator<CreateOrderDto>> _createValidatorMock;
    private readonly Mock<IValidator<GetOrdersDto>> _getOrdersValidatorMock;
    private readonly OrdersController _controller;

    public OrdersControllerTests()
    {
        _orderServiceMock = new Mock<IOrderService>();
        _createValidatorMock = new Mock<IValidator<CreateOrderDto>>();
        _getOrdersValidatorMock = new Mock<IValidator<GetOrdersDto>>();

        _controller = new OrdersController(
            _orderServiceMock.Object,
            _createValidatorMock.Object,
            _getOrdersValidatorMock.Object
        );
    }

    [Fact]
    public async Task CreateOrder_ShouldReturnOk_WhenDtoIsValid()
    {
        // Arrange
        var dto = new CreateOrderDto(Guid.NewGuid(), new List<CreateOrderItemDto>());
        var expectedOrderId = Guid.NewGuid();

        _createValidatorMock
            .Setup(v => v.ValidateAsync(dto, default))
            .ReturnsAsync(new ValidationResult()); // Валидация успешна

        _orderServiceMock
            .Setup(s => s.CreateOrderAsync(dto))
            .ReturnsAsync(expectedOrderId);

        // Act
        var result = await _controller.CreateOrder(dto);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(new { orderId = expectedOrderId });
    }

    [Fact]
    public async Task CreateOrder_ShouldReturnBadRequest_WhenDtoIsInvalid()
    {
        // Arrange
        var dto = new CreateOrderDto(Guid.Empty, new List<CreateOrderItemDto>());
        var failures = new List<ValidationFailure> { new("UserId", "UserId не должен быть пустым.") };
        
        _createValidatorMock
            .Setup(v => v.ValidateAsync(dto, default))
            .ReturnsAsync(new ValidationResult(failures));

        // Act
        var result = await _controller.CreateOrder(dto);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        _orderServiceMock.Verify(s => s.CreateOrderAsync(It.IsAny<CreateOrderDto>()), Times.Never);
    }

    [Fact]
    public async Task GetOrder_ShouldReturnOkWithDto_WhenOrderExists()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var order = new Order 
        { 
            Id = orderId, 
            UserId = Guid.NewGuid(), 
            TotalPrice = 150m,
            Items = new List<OrderItem>()
        };

        _orderServiceMock
            .Setup(s => s.GetOrderAsync(orderId))
            .ReturnsAsync(order);

        // Act
        var result = await _controller.GetOrder(orderId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedDto = okResult.Value.Should().BeOfType<OrderDto>().Subject;
        returnedDto.Id.Should().Be(orderId);
    }

    [Fact]
    public async Task GetOrder_ShouldReturnNotFound_WhenOrderDoesNotExist()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        _orderServiceMock
            .Setup(s => s.GetOrderAsync(orderId))
            .ReturnsAsync((Order?)null);

        // Act
        var result = await _controller.GetOrder(orderId);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task UpdateStatus_ShouldReturnNoContent()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var dto = new UpdateOrderStatusDto(OrderStatus.Shipped);

        // Act
        var result = await _controller.UpdateStatus(orderId, dto);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        _orderServiceMock.Verify(s => s.UpdateOrderStatusAsync(orderId, OrderStatus.Shipped), Times.Once);
    }
}