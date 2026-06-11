// Marketplace.Orders.Infrastructure/Implementation/MockProductGrpcClient.cs
using Marketplace.Orders.Application;

namespace Marketplace.Orders.Infrastructure.Implementation;

public class MockProductGrpcClient : IProductGrpcClient
{
    public Task<ProductInfo?> GetProductByIdAsync(Guid productId)
    {
        // Всегда говорим, что товар есть, цена 99.99
        return Task.FromResult<ProductInfo?>(
            new ProductInfo(productId, 99.99m));
    }
}