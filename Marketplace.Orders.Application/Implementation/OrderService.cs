using Marketplace.Orders.Application.DTOs;
using Marketplace.Orders.Domain;

namespace Marketplace.Orders.Application.Implementation;

public class OrderService(
    IOrderRepository orderRepository,
    IProductGrpcClient productClient)
{
    public async Task<Guid> CreateOrderAsync(CreateOrderDto dto)
    {
        var order = new Order
        {
            Id = Guid.NewGuid(),
            UserId = dto.UserId,
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
}