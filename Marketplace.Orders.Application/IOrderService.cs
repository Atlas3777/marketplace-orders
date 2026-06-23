using Marketplace.Orders.Application.DTOs;
using Marketplace.Orders.Domain;

namespace Marketplace.Orders.Application;

public interface IOrderService
{
    Task<Guid> CreateOrderAsync(CreateOrderDto dto);
    Task<Order?> GetOrderAsync(Guid orderId);
    Task<IEnumerable<Order>> GetOrdersPagedAsync(Guid userId, int pageIndex, int pageSize);
    Task UpdateOrderStatusAsync(Guid orderId, OrderStatus status);
}