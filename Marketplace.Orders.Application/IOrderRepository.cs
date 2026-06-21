using Marketplace.Orders.Domain;

namespace Marketplace.Orders.Application;

public interface IOrderRepository
{
    Task AddAsync(Order order);
    Task<Order?> GetByIdAsync(Guid id);
    Task<IEnumerable<Order>> GetPagedAsync(Guid userId, int offset, int limit);
    Task UpdateStatusAsync(Guid id, OrderStatus status);
}