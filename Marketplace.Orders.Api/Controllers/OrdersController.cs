using Microsoft.AspNetCore.Mvc;
using Marketplace.Orders.Application.DTOs;
using Marketplace.Orders.Application.Implementation;
using FluentValidation;
using Marketplace.Orders.Api.Mappers;
using Marketplace.Orders.Application;

namespace Marketplace.Orders.Api.Controllers;
[ApiController]
[Route("api/v1/orders")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly IValidator<CreateOrderDto> _createValidator;
    private readonly IValidator<GetOrdersDto> _getOrdersValidator;

    public OrdersController(
        IOrderService orderService, 
        IValidator<CreateOrderDto> createValidator,
        IValidator<GetOrdersDto> getOrdersValidator)
    {
        _orderService = orderService;
        _createValidator = createValidator;
        _getOrdersValidator = getOrdersValidator;
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDto dto)
    {
        var validationResult = await _createValidator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            return BadRequest(validationResult.ToDictionary());
        }

        var orderId = await _orderService.CreateOrderAsync(dto);
        return Ok(new { orderId });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetOrder(Guid id)
    {
        var order = await _orderService.GetOrderAsync(id);
        if (order == null)
            return NotFound();
            
        return Ok(order.ToDto());
    }

    [HttpGet]
    public async Task<IActionResult> GetOrders([FromQuery] GetOrdersDto query)
    {
        var validationResult = await _getOrdersValidator.ValidateAsync(query);
        if (!validationResult.IsValid)
        {
            return BadRequest(validationResult.ToDictionary());
        }

        var orders = await _orderService.GetOrdersPagedAsync(query.UserId, query.PageIndex, query.PageSize);

        var ordersDtos = orders.Select(x => x.ToDto());
        
        return Ok(ordersDtos);
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateOrderStatusDto dto)
    {
        await _orderService.UpdateOrderStatusAsync(id, dto.Status);
        return NoContent(); 
    }
}