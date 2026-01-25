using AudioStore.Common.DTOs.Cart;
using FluentValidation;

namespace AudioStore.Application.Validators.Cart;

public class AddToCartValidator : AbstractValidator<AddToCartDTO>
{
    public AddToCartValidator()
    {
        RuleFor(x => x)
            .Must(x => x.UserId.HasValue || !string.IsNullOrEmpty(x.SessionId))
            .WithMessage("Devi fornire UserId o SessionId");

        RuleFor(x => x.ProductId)
            .GreaterThan(0).WithMessage("ProductId non valido");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("La quantità deve essere maggiore di zero")
            .LessThanOrEqualTo(100).WithMessage("Quantità massima: 100");
    }
}
