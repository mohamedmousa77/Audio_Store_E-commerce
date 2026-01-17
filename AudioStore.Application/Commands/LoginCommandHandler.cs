using AudioStore.Application.DTOs.Auth;
using AudioStore.Common.Result;
using AudioStore.Domain.Entities;
using AudioStore.Domain.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Text;

namespace AudioStore.Application.Commands
{
    internal class LoginCommandHandler : IRequestHandler<LoginCommand, Result<LoginResponseDTO>>
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly IJwtTokenService _jwtTokenService;

        public LoginCommandHandler(
            UserManager<User> userManager,
        SignInManager<User> signInManager,
        IJwtTokenService jwtTokenService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _jwtTokenService = jwtTokenService;            
        }
        public async Task<Result<LoginResponseDTO>> Handle(
       LoginCommand request,
       CancellationToken cancellationToken)
        {
            // ✅ La validazione è già stata fatta dal ValidationBehavior!
            // Non serve più validare qui

            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
                return Result.Failure<LoginResponseDTO>("Credenziali non valide");

            var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, false);
            if (!result.Succeeded)
                return Result.Failure<LoginResponseDTO>("Credenziali non valide");

            var accessToken = await _jwtTokenService.GenerateAccessTokenAsync(user);
            var roles = await _userManager.GetRolesAsync(user);

            var response = new LoginResponseDTO
            {
                UserId = user.Id,
                Email = user.Email!,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Roles = roles,
                Token = new TokenResponseDTO
                {
                    AccessToken = accessToken,
                    RefreshToken = "", // TODO: Generate refresh token
                    ExpiresAt = DateTime.UtcNow.AddMinutes(60)
                }
            };

            return Result.Success(response);
        }
    }
}
