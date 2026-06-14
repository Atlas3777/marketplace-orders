using FluentValidation.TestHelper;
using Marketplace.Orders.Application.DTOs;
using Marketplace.Orders.Application.Validators;
using Xunit;

namespace Marketplace.Orders.UnitTests.ValidatorTests;

public class ValidatorTests
{
    private readonly CreateOrderDtoValidator _orderValidator = new();
    private readonly GetOrdersDtoValidator _pagedValidator = new();

    [Fact]
    public void CreateOrderDtoValidator_ShouldHaveErrors_WhenFieldsAreEmpty()
    {
        // Arrange
        var dto = new CreateOrderDto(Guid.Empty, []);

        // Act
        var result = _orderValidator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.UserId)
            .WithErrorMessage("UserId не должен быть пустым.");
            
        result.ShouldHaveValidationErrorFor(x => x.Items)
            .WithErrorMessage("Список товаров (Items) не может быть пустым.");
    }

    [Fact]
    public void CreateOrderDtoValidator_ShouldHaveErrors_WhenItemQuantityIsZeroOrLess()
    {
        // Arrange
        var dto = new CreateOrderDto(Guid.NewGuid(), new List<CreateOrderItemDto>
        {
            new(Guid.NewGuid(), 0),  // Ошибка тут
            new(Guid.NewGuid(), -5)  // И тут
        });

        // Act
        var result = _orderValidator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor("Items[0].Quantity")
            .WithErrorMessage("Количество товара должно быть больше 0.");
            
        result.ShouldHaveValidationErrorFor("Items[1].Quantity")
            .WithErrorMessage("Количество товара должно быть больше 0.");
    }

    [Theory]
    [InlineData(-1, 10, "PageIndex")] // Неверный индекс страницы
    [InlineData(0, 0, "PageSize")]   // Слишком маленький размер
    [InlineData(0, 101, "PageSize")] // Слишком большой размер
    public async Task GetOrdersDtoValidator_ShouldHaveErrors_WhenPaginationIsInvalid(
        int pageIndex, int pageSize, string expectedErrorField)
    {
        // Arrange
        var dto = new GetOrdersDto(pageIndex, pageSize);

        // Act
        var result = await _pagedValidator.TestValidateAsync(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(expectedErrorField);
    }

    [Fact]
    public void CreateOrderDtoValidator_ShouldBeValid_WhenDataIsCorrect()
    {
        // Arrange
        var dto = new CreateOrderDto(Guid.NewGuid(), new List<CreateOrderItemDto>
        {
            new(Guid.NewGuid(), 1)
        });

        // Act
        var result = _orderValidator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}