using AudioStore.Application.DTOs.Orders;
using FluentValidation;

namespace AudioStore.Application.Validators.Orders;

public class CreateOrderValidator : AbstractValidator<CreateOrderDTO>
{
    public CreateOrderValidator()
    {
        //  Guest checkout validation
        When(x => !x.UserId.HasValue, () =>
        {
            RuleFor(x => x.CustomerFirstName)
                .NotEmpty().WithMessage("Il nome è obbligatorio per guest checkout")
                .MaximumLength(100);

            RuleFor(x => x.CustomerLastName)
                .NotEmpty().WithMessage("Il cognome è obbligatorio per guest checkout")
                .MaximumLength(100);

            RuleFor(x => x.CustomerEmail)
                .NotEmpty().WithMessage("L'email è obbligatoria per guest checkout")
                .EmailAddress().WithMessage("Formato email non valido");

            RuleFor(x => x.CustomerPhone)
                .NotEmpty().WithMessage("Il telefono è obbligatorio per guest checkout")
                .MaximumLength(20);
        });

        //  Shipping address validation (sempre obbligatorio)
        RuleFor(x => x.ShippingStreet)
            .NotEmpty().WithMessage("L'indirizzo di spedizione è obbligatorio")
            .MaximumLength(200);

        RuleFor(x => x.ShippingCity)
            .NotEmpty().WithMessage("La città è obbligatoria")
            .MaximumLength(100);

        RuleFor(x => x.ShippingPostalCode)
            .NotEmpty().WithMessage("Il codice postale è obbligatorio")
            .MaximumLength(20);

        RuleFor(x => x.ShippingCountry)
            .NotEmpty().WithMessage("Il paese è obbligatorio")
            .MaximumLength(100);

        //  Items validation
        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("Il carrello è vuoto")
            .Must(items => items.Count > 0).WithMessage("Devi avere almeno un prodotto nel carrello");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ProductId)
                .GreaterThan(0).WithMessage("ProductId non valido");

            item.RuleFor(i => i.Quantity)
                .GreaterThan(0).WithMessage("Quantità non valida");

            item.RuleFor(i => i.UnitPrice)
                .GreaterThan(0).WithMessage("Prezzo non valido");
        });
    }

}
