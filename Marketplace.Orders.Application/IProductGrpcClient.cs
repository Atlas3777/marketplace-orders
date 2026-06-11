// Marketplace.Orders.Application/IProductGrpcClient.cs
namespace Marketplace.Orders.Application;

public interface IProductGrpcClient
{
    Task<ProductInfo?> GetProductByIdAsync(Guid productId);
}

// Простая модель — только то, что нужно сервису заказов
public record ProductInfo(Guid Id, decimal Price);