using AudioStore.Application.Services.Implementations;
using AudioStore.Common;
using AudioStore.Common.Constants;
using AudioStore.Common.DTOs.Profile;
using AudioStore.Domain.Entities;
using AudioStore.Domain.Interfaces;
using AudioStore.Tests.Helpers;
using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AudioStore.Tests.UnitTests.Services;

/// <summary>
/// Unit tests for ProfileService
/// Tests profile management operations (simplified - excluding Query() operations)
/// </summary>
public class ProfileServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly Mock<IPasswordHasher> _passwordHasherMock;
    private readonly Mock<ILogger<ProfileService>> _loggerMock;
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly ProfileService _profileService;

    public ProfileServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _mapperMock = new Mock<IMapper>();
        _passwordHasherMock = new Mock<IPasswordHasher>();
        _loggerMock = new Mock<ILogger<ProfileService>>();
        _userRepositoryMock = new Mock<IUserRepository>();

        _unitOfWorkMock.Setup(x => x.Users).Returns(_userRepositoryMock.Object);

        _profileService = new ProfileService(
            _unitOfWorkMock.Object,
            _mapperMock.Object,
            _passwordHasherMock.Object,
            _loggerMock.Object);
    }

    #region GetProfileAsync Tests

    [Fact]
    public async Task GetProfileAsync_WithExistingUser_ReturnsProfile()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            FirstName = "Mario",
            LastName = "Rossi",
            Email = "mario@test.com",
            PhoneNumber = "1234567890",
            Addresses = new List<Address>()
        };

        _userRepositoryMock.Setup(x => x.GetUserWithAddressesAsync(1))
            .ReturnsAsync(user);

        _mapperMock.Setup(x => x.Map<AddressDTO>(It.IsAny<Address>()))
            .Returns((AddressDTO?)null);

        // Act
        var result = await _profileService.GetProfileAsync(1);

        // Assert
        result.Should().BeSuccess();
        result.Should().HaveData();
        result.Value.Should().NotBeNull();
        result.Value!.Id.Should().Be(1);
        result.Value.FirstName.Should().Be("Mario");
    }

    [Fact]
    public async Task GetProfileAsync_WithNonExistentUser_ReturnsNotFound()
    {
        // Arrange
        _userRepositoryMock.Setup(x => x.GetUserWithAddressesAsync(999))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _profileService.GetProfileAsync(999);

        // Assert
        result.Should().BeFailure();
        result.Should().HaveErrorCode(ErrorCode.UserNotFound);
    }

    #endregion

    #region UpdateProfileAsync Tests

    [Fact]
    public async Task UpdateProfileAsync_WithValidData_UpdatesProfile()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            FirstName = "Mario",
            LastName = "Rossi",
            Email = "mario@test.com",
            Addresses = new List<Address>()
        };

        var updateDto = new UpdateProfileDTO
        {
            FirstName = "Luigi",
            LastName = "Verdi",
            PhoneNumber = "9876543210"
        };

        _userRepositoryMock.Setup(x => x.GetUserWithAddressesAsync(1))
            .ReturnsAsync(user);

        _unitOfWorkMock.Setup(x => x.SaveChangesAsync())
            .ReturnsAsync(1);

        _mapperMock.Setup(x => x.Map<AddressDTO>(It.IsAny<Address>()))
            .Returns((AddressDTO?)null);

        // Act
        var result = await _profileService.UpdateProfileAsync(1, updateDto);

        // Assert
        result.Should().BeSuccess();
        result.Should().HaveData();

        _userRepositoryMock.Verify(x => x.Update(It.IsAny<User>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateProfileAsync_WithNonExistentUser_ReturnsNotFound()
    {
        // Arrange
        var updateDto = new UpdateProfileDTO
        {
            FirstName = "Test",
            LastName = "User"
        };

        _userRepositoryMock.Setup(x => x.GetUserWithAddressesAsync(999))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _profileService.UpdateProfileAsync(999, updateDto);

        // Assert
        result.Should().BeFailure();
        result.Should().HaveErrorCode(ErrorCode.UserNotFound);

        _userRepositoryMock.Verify(x => x.Update(It.IsAny<User>()), Times.Never);
    }

    #endregion

    #region GetUserAddressesAsync Tests

    [Fact]
    public async Task GetUserAddressesAsync_WithExistingAddresses_ReturnsAddresses()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Addresses = new List<Address>
            {
                new Address
                {
                    Id = 1,
                    UserId = 1,
                    Street = "Via Roma 123",
                    City = "Milano",
                    PostalCode = "20100"
                },
                new Address
                {
                    Id = 2,
                    UserId = 1,
                    Street = "Via Verdi 456",
                    City = "Roma",
                    PostalCode = "00100"
                }
            }
        };

        var addressDtos = new List<AddressDTO>
        {
            new AddressDTO { Id = 1, Street = "Via Roma 123", City = "Milano" },
            new AddressDTO { Id = 2, Street = "Via Verdi 456", City = "Roma" }
        };

        _userRepositoryMock.Setup(x => x.GetUserWithAddressesAsync(1))
            .ReturnsAsync(user);

        _mapperMock.Setup(x => x.Map<IEnumerable<AddressDTO>>(user.Addresses))
            .Returns(addressDtos);

        // Act
        var result = await _profileService.GetUserAddressesAsync(1);

        // Assert
        result.Should().BeSuccess();
        result.Should().HaveData();
        result.Value.Should().NotBeNull();
        result.Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetUserAddressesAsync_WithNoAddresses_ReturnsEmptyList()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Addresses = new List<Address>()
        };

        _userRepositoryMock.Setup(x => x.GetUserWithAddressesAsync(1))
            .ReturnsAsync(user);

        _mapperMock.Setup(x => x.Map<IEnumerable<AddressDTO>>(user.Addresses))
            .Returns(new List<AddressDTO>());

        // Act
        var result = await _profileService.GetUserAddressesAsync(1);

        // Assert
        result.Should().BeSuccess();
        result.Should().HaveData();
        result.Value.Should().NotBeNull();
        result.Value.Should().BeEmpty();
    }

    #endregion
}
