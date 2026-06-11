using Marketplace.Orders.Domain;

namespace Marketplace.Orders.Application;

public interface IOrderRepository
{
    Task AddAsync(Order order);
    Task<Order?> GetByIdAsync(Guid id);
}