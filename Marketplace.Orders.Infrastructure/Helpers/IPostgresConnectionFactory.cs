using Npgsql;

namespace Marketplace.Orders.Infrastructure.Helpers;

public interface IPostgresConnectionFactory
{
    public NpgsqlConnection GetConnection();
}