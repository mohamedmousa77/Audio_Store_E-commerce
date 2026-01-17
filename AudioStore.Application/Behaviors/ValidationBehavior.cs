using AudioStore.Common.Result;
using FluentValidation;
using MediatR;

namespace AudioStore.Application.Behaviors;

public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : Result
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Se non ci sono validator registrati, continua
        if (!_validators.Any())
            return await next();

        var context = new ValidationContext<TRequest>(request);

        // Esegui tutte le validazioni in parallelo
        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        // Raccogli tutti gli errori
        var failures = validationResults
            .Where(r => !r.IsValid)
            .SelectMany(r => r.Errors)
            .ToList();

        // Se ci sono errori, restituisci un Result.Failure
        if (failures.Any())
        {
            var errors = string.Join("; ", failures.Select(f => f.ErrorMessage));

            // ✅ Crea una risposta Result.Failure del tipo corretto
            return (TResponse)(object)Result.Failure<TResponse>(
                errors,
                "VALIDATION_ERROR");
        }

        // Validazione superata, continua con il prossimo behavior/handler
        return await next();
    }

}
