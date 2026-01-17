using AudioStore.Application.DTOs.Auth;
using FluentValidation;

namespace AudioStore.Application.Validators.Auth;

public class LoginRequestValidator : AbstractValidator<LoginRequestDTO>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("L'email è obbligatoria")
            .EmailAddress().WithMessage("Formato email non valido");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("La password è obbligatoria");
    }
}
