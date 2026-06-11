namespace Marketplace.Orders.Application.DTOs;

public record CreateOrderItemDto(Guid ProductId, int Quantity);