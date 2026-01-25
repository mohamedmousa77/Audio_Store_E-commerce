using AudioStore.Common;
using AudioStore.Common.DTOs.Auth;
using MediatR;

namespace AudioStore.Application.Commands;

public class LoginCommand : IRequest<Result<LoginResponseDTO>>
{
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}
