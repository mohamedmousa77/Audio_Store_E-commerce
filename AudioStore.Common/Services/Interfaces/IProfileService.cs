using AudioStore.Common.DTOs.Orders;
using AudioStore.Common.DTOs.Profile;

namespace AudioStore.Common.Services.Interfaces;

public interface IProfileService
{
    // Profile
    Task<Result<UserProfileDTO>> GetProfileAsync(int userId);
    Task<Result<UserProfileDTO>> UpdateProfileAsync(int userId, UpdateProfileDTO dto);

    // Password
    Task<Result> ChangePasswordAsync(int userId, ChangePasswordDTO dto);

    // Address
    Task<Result<AddressDTO>> SaveAddressAsync(int userId, SaveAddressDTO dto);
    Task<Result<IEnumerable<AddressDTO>>> GetUserAddressesAsync(int userId);
    Task<Result> DeleteAddressAsync(int userId, int addressId);

    // Orders History
    Task<Result<IEnumerable<OrderDTO>>> GetUserOrdersAsync(int userId);
}
