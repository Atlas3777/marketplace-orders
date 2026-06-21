namespace Marketplace.Orders.Application.DTOs;

public record GetOrdersDto(Guid UserId, int PageIndex = 0, int PageSize = 10);