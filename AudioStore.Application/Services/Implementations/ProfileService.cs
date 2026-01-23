using AudioStore.Application.DTOs.Orders;
using AudioStore.Application.DTOs.Profile;
using AudioStore.Application.Services.Interfaces;
using AudioStore.Common.Constants;
using AudioStore.Common.Result;
using AudioStore.Domain.Entities;
using AudioStore.Domain.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AudioStore.Application.Services.Implementations;

public class ProfileService : IProfileService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<ProfileService> _logger;

    public ProfileService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IPasswordHasher passwordHasher,
        ILogger<ProfileService> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task<Result<UserProfileDTO>> GetProfileAsync(int userId)
    {
        try
        {
            var user = await _unitOfWork.Users.GetUserWithAddressesAsync(userId);

            if (user == null)
            {
                return Result.Failure<UserProfileDTO>(
                    "Utente non trovato",
                    ErrorCode.UserNotFound);
            }

            var defaultAddress = user.Addresses.FirstOrDefault(a => a.IsDefault);

            var profile = new UserProfileDTO
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email!,
                PhoneNumber = user.PhoneNumber,
                RegistrationDate = user.CreatedAt,
                DefaultAddress = defaultAddress != null ? _mapper.Map<AddressDTO>(defaultAddress) : null
            };

            return Result.Success(profile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting profile for user {UserId}", userId);
            return Result.Failure<UserProfileDTO>(
                "Errore recupero profilo",
                ErrorCode.InternalServerError);
        }
    }

    public async Task<Result<UserProfileDTO>> UpdateProfileAsync(
        int userId, UpdateProfileDTO dto)
    {
        try
        {
            var user = await _unitOfWork.Users.GetUserWithAddressesAsync(userId);

            if (user == null)
            {
                return Result.Failure<UserProfileDTO>(
                    "Utente non trovato",
                    ErrorCode.UserNotFound);
            }

            user.FirstName = dto.FirstName;
            user.LastName = dto.LastName;
            user.PhoneNumber = dto.PhoneNumber;
            user.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Users.Update(user);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Profile updated for user {UserId}", userId);

            return await GetProfileAsync(userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating profile for user {UserId}", userId);
            return Result.Failure<UserProfileDTO>(
                "Errore aggiornamento profilo",
                ErrorCode.InternalServerError);
        }
    }

    public async Task<Result> ChangePasswordAsync(int userId, ChangePasswordDTO dto)
    {
        try
        {
            if (dto.NewPassword != dto.ConfirmNewPassword)
            {
                return Result.Failure(
                    "Le password non corrispondono",
                    ErrorCode.ValidationError);
            }

            var user = await _unitOfWork.Users.GetByIdAsync(userId);
            if (user == null)
            {
                return Result.Failure(
                    "Utente non trovato",
                    ErrorCode.UserNotFound);
            }

            // Verify current password
            if (!_passwordHasher.VerifyPassword(dto.CurrentPassword, user.PasswordHash))
            {
                return Result.Failure(
                    "Password attuale non corretta",
                    ErrorCode.InvalidCredentials);
            }

            // Hash new password
            user.PasswordHash = _passwordHasher.HashPassword(dto.NewPassword);
            user.UpdatedAt = DateTime.UtcNow;

             _unitOfWork.Users.Update(user);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Password changed for user {UserId}", userId);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password for user {UserId}", userId);
            return Result.Failure(
                "Errore cambio password",
                ErrorCode.InternalServerError);
        }
    }

    public async Task<Result<AddressDTO>> SaveAddressAsync(int userId, SaveAddressDTO dto)
    {
        try
        {
            var user = await _unitOfWork.Users
                .Query()
                .Include(u => u.Addresses)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return Result.Failure<AddressDTO>(
                    "Utente non trovato",
                    ErrorCode.UserNotFound);
            }

            Address address;

            if (dto.AddressId.HasValue)
            {
                // Update existing address
                address = user.Addresses.FirstOrDefault(a => a.Id == dto.AddressId.Value);
                if (address == null)
                {
                    return Result.Failure<AddressDTO>(
                        "Indirizzo non trovato",
                        ErrorCode.NotFound);
                }

                address.Street = dto.Street;
                address.City = dto.City;
                address.PostalCode = dto.PostalCode;
                address.Country = dto.Country;
                address.UpdatedAt = DateTime.UtcNow;

                 _unitOfWork.Addresses.Update(address);
            }
            else
            {
                // Create new address
                address = new Address
                {
                    UserId = userId,
                    Street = dto.Street,
                    City = dto.City,
                    PostalCode = dto.PostalCode,
                    Country = dto.Country,
                    IsDefault = dto.SetAsDefault,
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.Addresses.AddAsync(address);
            }

            // Set as default if requested
            if (dto.SetAsDefault)
            {
                foreach (var addr in user.Addresses.Where(a => a.Id != address.Id))
                {
                    addr.IsDefault = false;
                    _unitOfWork.Addresses.Update(addr);
                }
                address.IsDefault = true;
            }

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Address saved for user {UserId}", userId);

            var addressDto = _mapper.Map<AddressDTO>(address);
            return Result.Success(addressDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving address for user {UserId}", userId);
            return Result.Failure<AddressDTO>(
                "Errore salvataggio indirizzo",
                ErrorCode.InternalServerError);
        }
    }

    public async Task<Result<IEnumerable<AddressDTO>>> GetUserAddressesAsync(int userId)
    {
        try
        {
            var addresses = await _unitOfWork.Addresses
                .Query()
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.IsDefault)
                .ThenByDescending(a => a.CreatedAt)
                .ToListAsync();

            var addressDtos = _mapper.Map<IEnumerable<AddressDTO>>(addresses);
            return Result.Success(addressDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting addresses for user {UserId}", userId);
            return Result.Failure<IEnumerable<AddressDTO>>(
                "Errore recupero indirizzi",
                ErrorCode.InternalServerError);
        }
    }

    public async Task<Result> DeleteAddressAsync(int userId, int addressId)
    {
        try
        {
            var address = await _unitOfWork.Addresses
                .Query()
                .FirstOrDefaultAsync(a => a.Id == addressId && a.UserId == userId);

            if (address == null)
            {
                return Result.Failure(
                    "Indirizzo non trovato",
                    ErrorCode.NotFound);
            }

             _unitOfWork.Addresses.Delete(address);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Address {AddressId} deleted for user {UserId}", addressId, userId);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting address {AddressId} for user {UserId}", addressId, userId);
            return Result.Failure(
                "Errore eliminazione indirizzo",
                ErrorCode.InternalServerError);
        }
    }

    public async Task<Result<IEnumerable<OrderDTO>>> GetUserOrdersAsync(int userId)
    {
        try
        {
            var orders = await _unitOfWork.Orders.GetUserOrdersAsync(userId);
                //.Query()
                //.Include(o => o.OrderItems)
                //.ThenInclude(oi => oi.Product)
                //.Where(o => o.UserId == userId)
                //.OrderByDescending(o => o.OrderDate)
                //.ToListAsync();

            var orderDtos = _mapper.Map<IEnumerable<OrderDTO>>(orders);
            return Result.Success(orderDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting orders for user {UserId}", userId);
            return Result.Failure<IEnumerable<OrderDTO>>(
                "Errore recupero ordini",
                ErrorCode.InternalServerError);
        }
    }

}
