using AudioStore.Common.DTOs.Auth;
using FluentValidation;

namespace AudioStore.Application.Validators.Auth;

public class RegisterRequestValidator : AbstractValidator<RegisterRequestDTO>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("Il nome è obbligatorio")
            .MaximumLength(100).WithMessage("Il nome non può superare 100 caratteri");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Il cognome è obbligatorio")
            .MaximumLength(100).WithMessage("Il cognome non può superare 100 caratteri");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("L'email è obbligatoria")
            .EmailAddress().WithMessage("Formato email non valido");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("La password è obbligatoria")
            .MinimumLength(8).WithMessage("La password deve contenere almeno 8 caratteri")
            .Matches(@"[A-Z]").WithMessage("La password deve contenere almeno una maiuscola")
            .Matches(@"[a-z]").WithMessage("La password deve contenere almeno una minuscola")
            .Matches(@"[0-9]").WithMessage("La password deve contenere almeno un numero")
            .Matches(@"[\W]").WithMessage("La password deve contenere almeno un carattere speciale");

        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.Password).WithMessage("Le password non corrispondono");
    }
}
