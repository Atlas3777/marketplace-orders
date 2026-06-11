using Marketplace.Orders.Application;
using Marketplace.Products.Api.Protos;

namespace Marketplace.Orders.Infrastructure.Implementation;

public class ProductGrpcClient : IProductGrpcClient
{
    private readonly ProductServiceGrpc.ProductServiceGrpcClient _client;

    public ProductGrpcClient(ProductServiceGrpc.ProductServiceGrpcClient client)
    {
        _client = client;
    }

    public async Task<ProductInfo?> GetProductByIdAsync(Guid productId)
    {
        var request = new GetProductByIdRequest { Id = productId.ToString() };
        var response = await _client.GetProductByIdAsync(request);

        if (response?.Product is null)
            return null;

        return new ProductInfo(
            Id: Guid.Parse(response.Product.Id),
            Price: ConvertDecimalValue(response.Product.Price)
        );
    }

    private static decimal ConvertDecimalValue(DecimalValue decimalValue)
    {
        return decimalValue.Units + (decimal)decimalValue.Nanos / 1_000_000_000m;
    }
}