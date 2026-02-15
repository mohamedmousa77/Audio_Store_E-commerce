using AudioStore.Application.Services.Implementations;
using AudioStore.Common.Constants;
using AudioStore.Common.DTOs.Auth;
using AudioStore.Common.Services.Interfaces;
using AudioStore.Domain.Entities;
using AudioStore.Domain.Interfaces;
using AudioStore.Tests.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AudioStore.Tests.UnitTests.Services;

/// <summary>
/// Unit tests for AuthService
/// NOTE: These tests are simplified due to the complexity of mocking UserManager and SignInManager.
/// Full authentication flow testing is recommended via integration tests.
/// </summary>
public class AuthServiceTests
{
    private readonly Mock<UserManager<User>> _userManagerMock;
    private readonly Mock<SignInManager<User>> _signInManagerMock;
    private readonly Mock<ITokenService> _tokenServiceMock;
    private readonly Mock<ILogger<AuthService>> _loggerMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IRepository<RefreshToken>> _refreshTokenRepositoryMock;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        // Mock UserManager (requires IUserStore)
        var userStoreMock = new Mock<IUserStore<User>>();
        _userManagerMock = new Mock<UserManager<User>>(
            userStoreMock.Object, null, null, null, null, null, null, null, null);

        // Mock SignInManager (requires UserManager, IHttpContextAccessor, IUserClaimsPrincipalFactory)
        var contextAccessorMock = new Mock<Microsoft.AspNetCore.Http.IHttpContextAccessor>();
        var claimsPrincipalFactoryMock = new Mock<IUserClaimsPrincipalFactory<User>>();
        _signInManagerMock = new Mock<SignInManager<User>>(
            _userManagerMock.Object,
            contextAccessorMock.Object,
            claimsPrincipalFactoryMock.Object,
            null, null, null, null);

        _tokenServiceMock = new Mock<ITokenService>();
        _loggerMock = new Mock<ILogger<AuthService>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _refreshTokenRepositoryMock = new Mock<IRepository<RefreshToken>>();

        _unitOfWorkMock.Setup(x => x.RefreshTokens).Returns(_refreshTokenRepositoryMock.Object);

        _authService = new AuthService(
            _userManagerMock.Object,
            _signInManagerMock.Object,
            _tokenServiceMock.Object,
            _loggerMock.Object,
            _unitOfWorkMock.Object);
    }

    #region LoginAsync Tests

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsSuccess()
    {
        // Arrange
        var loginRequest = new LoginRequestDTO
        {
            Email = "test@example.com",
            Password = "Password123!"
        };

        var user = new User
        {
            Id = 1,
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            IsActive = true,

        };

        _userManagerMock.Setup(x => x.FindByEmailAsync(loginRequest.Email))
            .ReturnsAsync(user);

        _signInManagerMock.Setup(x => x.CheckPasswordSignInAsync(user, loginRequest.Password, false))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);

        _tokenServiceMock.Setup(x => x.GenerateAccessTokenAsync(
            user.Id, user.Email!, user.FirstName, user.LastName,
             It.Is<IList<string>>(r => r.Contains(UserRole.Customer))
            ))
            .ReturnsAsync("access_token_123");

        _tokenServiceMock.Setup(x => x.GenerateRefreshToken())
            .Returns("refresh_token_456");

        _userManagerMock.Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(new List<string> { UserRole.Customer });

        _refreshTokenRepositoryMock.Setup(x => x.AddAsync(It.IsAny<RefreshToken>()))
            .ReturnsAsync((RefreshToken rt) => rt);

        _unitOfWorkMock.Setup(x => x.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        var result = await _authService.LoginAsync(loginRequest);

        // Assert
        result.Should().BeSuccess();
        result.Should().HaveData();
        result.Value.Should().NotBeNull();
        result.Value!.UserId.Should().Be(1);
        result.Value.Email.Should().Be("test@example.com");
        result.Value.Token.Should().NotBeNull();
        result.Value.Token.AccessToken.Should().Be("access_token_123");
        result.Value.Token.RefreshToken.Should().Be("refresh_token_456");
    }

    [Fact]
    public async Task LoginAsync_WithInvalidEmail_ReturnsFailure()
    {
        // Arrange
        var loginRequest = new LoginRequestDTO
        {
            Email = "nonexistent@example.com",
            Password = "Password123!"
        };

        _userManagerMock.Setup(x => x.FindByEmailAsync(loginRequest.Email))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _authService.LoginAsync(loginRequest);

        // Assert
        result.Should().BeFailure();
        result.Should().HaveErrorCode(ErrorCode.InvalidCredentials);
    }

    [Fact]
    public async Task LoginAsync_WithInvalidPassword_ReturnsFailure()
    {
        // Arrange
        var loginRequest = new LoginRequestDTO
        {
            Email = "test@example.com",
            Password = "WrongPassword!"
        };

        var user = new User
        {
            Id = 1,
            Email = "test@example.com",
            IsActive = true
        };

        _userManagerMock.Setup(x => x.FindByEmailAsync(loginRequest.Email))
            .ReturnsAsync(user);

        _signInManagerMock.Setup(x => x.CheckPasswordSignInAsync(user, loginRequest.Password, false))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Failed);

        // Act
        var result = await _authService.LoginAsync(loginRequest);

        // Assert
        result.Should().BeFailure();
        result.Should().HaveErrorCode(ErrorCode.InvalidCredentials);
    }

    [Fact]
    public async Task LoginAsync_WithInactiveUser_ReturnsUnauthorized()
    {
        // Arrange
        var loginRequest = new LoginRequestDTO
        {
            Email = "test@example.com",
            Password = "Password123!"
        };

        var user = new User
        {
            Id = 1,
            Email = "test@example.com",
            IsActive = false // Inactive user
        };

        _userManagerMock.Setup(x => x.FindByEmailAsync(loginRequest.Email))
            .ReturnsAsync(user);

        _signInManagerMock.Setup(x => x.CheckPasswordSignInAsync(user, loginRequest.Password, false))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);

        // Act
        var result = await _authService.LoginAsync(loginRequest);

        // Assert
        result.Should().BeFailure();
        result.Should().HaveErrorCode(ErrorCode.Unauthorized);
    }

    #endregion

    #region RegisterAsync Tests

    [Fact]
    public async Task RegisterAsync_WithValidData_CreatesUser()
    {
        // Arrange
        var registerRequest = new RegisterRequestDTO
        {
            Email = "newuser@example.com",
            Password = "Password123!",
            FirstName = "New",
            LastName = "User",
            PhoneNumber = "1234567890"
        };

        var createdUser = new User
        {
            Id = 1,
            Email = registerRequest.Email,
            FirstName = registerRequest.FirstName,
            LastName = registerRequest.LastName,
            IsActive = true
        };

        // Setup sequence for FindByEmailAsync: first null (doesn't exist), then return user (after creation)
        var findByEmailSequence = new Queue<User?>(new[] { (User?)null, createdUser });
        _userManagerMock.Setup(x => x.FindByEmailAsync(registerRequest.Email))
            .ReturnsAsync(() => findByEmailSequence.Dequeue());

        _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<User>(), registerRequest.Password))
            .ReturnsAsync(IdentityResult.Success)
            .Callback<User, string>((user, _) =>
            {
                user.Id = 1;
                user.IsActive = true;
            });

        _userManagerMock.Setup(x => x.AddToRoleAsync(It.IsAny<User>(), UserRole.Customer))
            .ReturnsAsync(IdentityResult.Success);

        _signInManagerMock.Setup(x => x.CheckPasswordSignInAsync(It.IsAny<User>(), registerRequest.Password, false))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);

        _tokenServiceMock.Setup(x => x.GenerateAccessTokenAsync(
            It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IList<string>>()))
            .ReturnsAsync("access_token");

        _tokenServiceMock.Setup(x => x.GenerateRefreshToken())
            .Returns("refresh_token");

        _userManagerMock.Setup(x => x.GetRolesAsync(It.IsAny<User>()))
            .ReturnsAsync(new List<string> { UserRole.Customer });

        _refreshTokenRepositoryMock.Setup(x => x.AddAsync(It.IsAny<RefreshToken>()))
            .ReturnsAsync((RefreshToken rt) => rt);

        _unitOfWorkMock.Setup(x => x.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        var result = await _authService.RegisterAsync(registerRequest);

        // Assert
        result.Should().BeSuccess();

        _userManagerMock.Verify(x => x.CreateAsync(It.IsAny<User>(), registerRequest.Password), Times.Once);
        _userManagerMock.Verify(x => x.AddToRoleAsync(It.IsAny<User>(), UserRole.Customer), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_WithExistingEmail_ReturnsEmailAlreadyExists()
    {
        // Arrange
        var registerRequest = new RegisterRequestDTO
        {
            Email = "existing@example.com",
            Password = "Password123!",
            FirstName = "Test",
            LastName = "User"
        };

        var existingUser = new User { Id = 1, Email = registerRequest.Email };

        _userManagerMock.Setup(x => x.FindByEmailAsync(registerRequest.Email))
            .ReturnsAsync(existingUser);

        // Act
        var result = await _authService.RegisterAsync(registerRequest);

        // Assert
        result.Should().BeFailure();
        result.Should().HaveErrorCode(ErrorCode.EmailAlreadyExists);

        _userManagerMock.Verify(x => x.CreateAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Never);
    }

    #endregion

    #region LogoutAsync Tests

    [Fact]
    public async Task LogoutAsync_CallsSignOutAsync()
    {
        // Arrange
        var userId = 1;

        _signInManagerMock.Setup(x => x.SignOutAsync())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _authService.LogoutAsync(userId);

        // Assert
        result.Should().BeSuccess();

        _signInManagerMock.Verify(x => x.SignOutAsync(), Times.Once);
    }

    #endregion
}
