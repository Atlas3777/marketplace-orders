namespace Marketplace.Orders.Application.DTOs;

public record CreateOrderDto(Guid UserId, List<CreateOrderItemDto> Items);