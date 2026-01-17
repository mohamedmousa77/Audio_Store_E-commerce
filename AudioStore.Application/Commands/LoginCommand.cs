using AudioStore.Application.DTOs.Auth;
using AudioStore.Common.Result;
using MediatR;

namespace AudioStore.Application.Commands;

public class LoginCommand : IRequest<Result<LoginResponseDTO>>
{
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}
