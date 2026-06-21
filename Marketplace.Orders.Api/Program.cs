using Marketplace.Orders.Application;
using Microsoft.Extensions.Caching.Distributed;
using Marketplace.Orders.Infrastructure.Helpers;
using Marketplace.Orders.Infrastructure.Implementation;
using Marketplace.Orders.Migrations.Migrations;
using Marketplace.Orders.Application.Validators;
using FluentValidation;
using Marketplace.Orders.Api.Middleware;
using Marketplace.Orders.Infrastructure.Cache;

var builder = WebApplication.CreateBuilder(args);

var ordersConnectionString = builder.Configuration.GetConnectionString("OrdersDb")
                             ?? throw new InvalidOperationException("OrdersDb connection string is missing.");

var redisConnectionString = builder.Configuration.GetConnectionString("Redis") 
                            ?? "localhost:6380";

var productsGrpcAddress = builder.Configuration["GrpcServices:Products"]
                          ?? "http://localhost:5107";

var services = builder.Services;

services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy =>
        {
            policy.WithOrigins("http://localhost:5173")
                .AllowAnyMethod()
                .AllowAnyHeader();
        });
});

services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = redisConnectionString;
    options.InstanceName = "MarketplaceOrders_"; 
});

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

services.AddScoped<OrderRepository>();
services.AddScoped<IOrderRepository>(provider => 
    new CachedOrderRepository(
        provider.GetRequiredService<OrderRepository>(),
        provider.GetRequiredService<IDistributedCache>()
    ));

services.AddValidatorsFromAssemblyContaining<CreateOrderDtoValidator>();

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseCors("AllowAll");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.RunMigrations();

app.MapControllers();

app.Run();