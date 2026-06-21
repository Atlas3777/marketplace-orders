using FluentValidation;
using Marketplace.Orders.Application.DTOs;

namespace Marketplace.Orders.Application.Validators;

public class GetOrdersDtoValidator : AbstractValidator<GetOrdersDto>
{
    public GetOrdersDtoValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId не должен быть пустым.");
        
        RuleFor(x => x.PageIndex)
            .GreaterThanOrEqualTo(0)
            .WithMessage("PageIndex должен быть больше или равен 0.");

        RuleFor(x => x.PageSize)
            .GreaterThan(0)
            .WithMessage("PageSize должен быть больше 0.")
            .LessThanOrEqualTo(100)
            .WithMessage("PageSize не может превышать 100 элементов.");
    }
}