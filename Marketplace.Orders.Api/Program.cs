using Marketplace.Orders.Application;
using Marketplace.Orders.Application.Implementation;
using Marketplace.Orders.Infrastructure;
using Marketplace.Orders.Infrastructure.Helpers;
using Marketplace.Orders.Infrastructure.Implementation;
using Marketplace.Orders.Migrations.Migrations; // важно!

var builder = WebApplication.CreateBuilder(args);

// ---------- Конфигурация ----------
var ordersConnectionString = builder.Configuration.GetConnectionString("OrdersDb")
                             ?? throw new InvalidOperationException("OrdersDb connection string is missing.");
var productsGrpcAddress = builder.Configuration["GrpcServices:Products"]
                          ?? "http://localhost:5107";

var services = builder.Services;
services.AddControllers();
services.AddEndpointsApiExplorer();
services.AddSwaggerGen();

services.AddSingleton<IPostgresConnectionFactory>(
    new PostgresConnectionFactory(ordersConnectionString));
services.AddScoped<IOrderRepository, OrderRepository>();

services.AddGrpcClient<Marketplace.Products.Api.Protos.ProductServiceGrpc.ProductServiceGrpcClient>(o =>
{
    o.Address = new Uri(productsGrpcAddress);
});

services.AddScoped<IProductGrpcClient, ProductGrpcClient>();

services.AddScoped<OrderService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.RunMigrations();

app.MapControllers();

app.Run();