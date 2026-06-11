using Dapper;
using Marketplace.Orders.Application;
using Marketplace.Orders.Domain;
using Marketplace.Orders.Infrastructure.Helpers;

namespace Marketplace.Orders.Infrastructure.Implementation;

public class OrderRepository : IOrderRepository
{
    private readonly IPostgresConnectionFactory _connectionFactory;

    public OrderRepository(IPostgresConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task AddAsync(Order order)
    {
        using var connection = _connectionFactory.GetConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        var orderSql = @"
            INSERT INTO orders (id, userid, totalprice, createdat)
            VALUES (@Id, @UserId, @TotalPrice, @CreatedAt)";

        await connection.ExecuteAsync(orderSql, order, transaction);

        var itemSql = @"
            INSERT INTO order_items (id, orderid, productid, quantity, price)
            VALUES (@Id, @OrderId, @ProductId, @Quantity, @Price)";

        await connection.ExecuteAsync(itemSql, order.Items, transaction);

        transaction.Commit();
    }

    public async Task<Order?> GetByIdAsync(Guid id)
    {
        using var connection = _connectionFactory.GetConnection();
        var order = await connection.QuerySingleOrDefaultAsync<Order>(
            "SELECT id, userid, totalprice, createdat FROM orders WHERE id = @Id", new { Id = id });

        if (order == null) return null;

        var items = await connection.QueryAsync<OrderItem>(
            "SELECT id, orderid, productid, quantity, price FROM order_items WHERE orderid = @OrderId",
            new { OrderId = id });

        order.Items = items.ToList();
        return order;
    }
}