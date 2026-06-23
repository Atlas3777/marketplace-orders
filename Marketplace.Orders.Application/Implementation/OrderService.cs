using Marketplace.Orders.Application.DTOs;
using Marketplace.Orders.Domain;

namespace Marketplace.Orders.Application.Implementation;

public class OrderService(
    IOrderRepository orderRepository,
    IProductGrpcClient productClient) : IOrderService
{
    public async Task<Guid> CreateOrderAsync(CreateOrderDto dto)
    {
        var order = new Order
        {
            Id = Guid.NewGuid(),
            UserId = dto.UserId,
            Status = OrderStatus.Created,
            CreatedAt = DateTime.UtcNow
        };

        decimal total = 0;

        foreach (var itemDto in dto.Items)
        {
            var productInfo = await productClient.GetProductByIdAsync(itemDto.ProductId);

            if (productInfo is null)
                throw new KeyNotFoundException($"Товар {itemDto.ProductId} не найден!");

            var orderItem = new OrderItem
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                ProductId = productInfo.Id,
                Quantity = itemDto.Quantity,
                Price = productInfo.Price
            };

            order.Items.Add(orderItem);
            total += productInfo.Price * itemDto.Quantity;
        }

        order.TotalPrice = total;
        await orderRepository.AddAsync(order);

        return order.Id;
    }
    
    public async Task<Order?> GetOrderAsync(Guid orderId)
    {
        return await orderRepository.GetByIdAsync(orderId);
    }

    public async Task<IEnumerable<Order>> GetOrdersPagedAsync(Guid userId, int pageIndex, int pageSize)
    {
        if (pageIndex < 0) pageIndex = 0;
        if (pageSize <= 0) pageSize = 10;

        int offset = pageIndex * pageSize;
        int limit = pageSize;

        return await orderRepository.GetPagedAsync(userId, offset, limit);
    }

    public async Task UpdateOrderStatusAsync(Guid orderId, OrderStatus status)
    {
        await orderRepository.UpdateStatusAsync(orderId, status);
    }
}