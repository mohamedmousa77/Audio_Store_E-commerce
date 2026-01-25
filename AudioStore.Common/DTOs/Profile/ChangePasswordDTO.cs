namespace AudioStore.Common.DTOs.Profile;

public record ChangePasswordDTO
{
    public string CurrentPassword { get; init; } = string.Empty;
    public string NewPassword { get; init; } = string.Empty;
    public string ConfirmNewPassword { get; init; } = string.Empty;

}
