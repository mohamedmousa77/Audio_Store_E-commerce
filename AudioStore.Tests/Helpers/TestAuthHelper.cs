using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AudioStore.Common.Constants;
using Microsoft.IdentityModel.Tokens;

namespace AudioStore.Tests.Helpers;

/// <summary>
/// Helper class for generating JWT tokens and authenticated HTTP clients in tests
/// </summary>
public static class TestAuthHelper
{
    private const string TestSecretKey = "ThisIsATestSecretKeyForJwtTokenGenerationInTests123456789";
    private const string TestIssuer = "AudioStoreTestAPI";
    private const string TestAudience = "AudioStoreTestClient";

    /// <summary>
    /// Generates a JWT token for testing purposes
    /// </summary>
    public static string GenerateJwtToken(
        int userId, 
        string email, 
        string role, 
        int expirationMinutes = 60)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Email, email),
            new(ClaimTypes.Role, role),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestSecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: TestIssuer,
            audience: TestAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Generates a JWT token for an admin user
    /// </summary>
    public static string GenerateAdminToken(int userId = 1, string email = "admin@test.com")
    {
        return GenerateJwtToken(userId, email, UserRole.Admin);
    }

    /// <summary>
    /// Generates a JWT token for a customer user
    /// </summary>
    public static string GenerateCustomerToken(int userId = 2, string email = "customer@test.com")
    {
        return GenerateJwtToken(userId, email, UserRole.Customer);
    }

    /// <summary>
    /// Creates an HttpClient with an admin authorization header
    /// </summary>
    public static HttpClient CreateAuthenticatedClient(HttpClient client, string token)
    {
        client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    /// <summary>
    /// Creates an HttpClient with admin credentials
    /// </summary>
    public static HttpClient CreateAdminClient(HttpClient client)
    {
        var token = GenerateAdminToken();
        return CreateAuthenticatedClient(client, token);
    }

    /// <summary>
    /// Creates an HttpClient with customer credentials
    /// </summary>
    public static HttpClient CreateCustomerClient(HttpClient client)
    {
        var token = GenerateCustomerToken();
        return CreateAuthenticatedClient(client, token);
    }

    /// <summary>
    /// Extracts user ID from a JWT token
    /// </summary>
    public static string? ExtractUserIdFromToken(string token)
    {
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        return jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
    }

    /// <summary>
    /// Validates if a token is expired
    /// </summary>
    public static bool IsTokenExpired(string token)
    {
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        return jwtToken.ValidTo < DateTime.UtcNow;
    }

    /// <summary>
    /// Gets the security key used for token generation (for test configuration)
    /// </summary>
    public static SymmetricSecurityKey GetTestSecurityKey()
    {
        return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestSecretKey));
    }

    /// <summary>
    /// Gets test issuer
    /// </summary>
    public static string GetTestIssuer() => TestIssuer;

    /// <summary>
    /// Gets test audience
    /// </summary>
    public static string GetTestAudience() => TestAudience;
}

