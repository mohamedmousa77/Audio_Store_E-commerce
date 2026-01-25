using AudioStore.Common.DTOs.Products;
using FluentValidation;

namespace AudioStore.Application.Validators.Products;

public class CreateProductValidator : AbstractValidator<CreateProductDTO>
{
    public CreateProductValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Il nome è obbligatorio")
            .MaximumLength(200).WithMessage("Il nome non può superare 200 caratteri");

        RuleFor(x => x.Brand)
            .NotEmpty().WithMessage("Il brand è obbligatorio")
            .MaximumLength(100).WithMessage("Il brand non può superare 100 caratteri");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("La descrizione è obbligatoria")
            .MinimumLength(20).WithMessage("La descrizione deve contenere almeno 20 caratteri");

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Il prezzo deve essere maggiore di zero")
            .LessThan(100000).WithMessage("Il prezzo non può superare €100.000");

        RuleFor(x => x.StockQuantity)
            .GreaterThanOrEqualTo(0).WithMessage("Lo stock non può essere negativo");

        RuleFor(x => x.MainImage)
            .NotEmpty().WithMessage("L'immagine principale è obbligatoria");

        RuleFor(x => x.CategoryId)
            .GreaterThan(0).WithMessage("La categoria è obbligatoria");

        RuleFor(x => x.GalleryImages)
            .Must(images => images == null || images.Count <= 5)
            .WithMessage("Massimo 5 immagini nella galleria");
    }
}
