using AudioStore.Application.Commands;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace AudioStore.Application.Validators.Auth;

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email obbligatoria")
            .EmailAddress().WithMessage("Formato email non valido");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password obbligatoria");
    }
}
