using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Moq;
using WhearApp.Infrastructure.Identity.Security;

namespace WhearApp.UnitTests;

public class JwtServiceTests : IDisposable
{
    private readonly Mock<IConfiguration> _configMock;
    private readonly Mock<IKeyManagementService> _keyServiceMock;
    private readonly System.Security.Cryptography.RSA _rsa;
    private readonly JwtService _jwtService;

    public JwtServiceTests()
    {
        // Setup configuration mock
        _configMock = new Mock<IConfiguration>();
        _configMock.Setup(c => c["Jwt:Issuer"]).Returns("TestIssuer");
        _configMock.Setup(c => c["Jwt:Audience"]).Returns("TestAudience");
        _configMock.Setup(c => c["Jwt:ExpirationMinutes"]).Returns("60");

        // Generate test RSA key pair and keep the RSA instance alive
        _rsa = System.Security.Cryptography.RSA.Create(2048);
        var parameters = _rsa.ExportParameters(true);
        var testPrivateKey = new RsaSecurityKey(parameters) { KeyId = "test-key-id" };
        var testPublicKey = new RsaSecurityKey(_rsa.ExportParameters(false)) { KeyId = "test-key-id" };

        // Setup key service mock
        _keyServiceMock = new Mock<IKeyManagementService>();
        _keyServiceMock.Setup(k => k.GetCurrentPrivateKey()).Returns(testPrivateKey);
        _keyServiceMock.Setup(k => k.GetCurrentPublicKey()).Returns(testPublicKey);
        _keyServiceMock.Setup(k => k.GetAllPublicKeys()).Returns([testPublicKey]);

        _jwtService = new JwtService(_configMock.Object, _keyServiceMock.Object);
        JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
    }

    public void Dispose()
    {
        _rsa.Dispose();
    }

    [Fact]
    public void GenerateToken_WithValidParameters_ShouldReturnValidToken()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var username = "testuser";
        var roles = new List<string> { "Admin", "User" };

        // Act
        var token = _jwtService.GenerateToken(userId, username, roles);

        // Assert
        Assert.NotNull(token);
        Assert.NotEmpty(token);
        
        // Verify token structure (JWT has 3 parts separated by dots)
        var tokenParts = token.Split('.');
        Assert.Equal(3, tokenParts.Length);
    }

    [Fact]
    public void GenerateToken_WithValidParameters_ShouldContainCorrectClaims()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var username = "testuser";
        var roles = new List<string> { "Admin", "User" };

        // Act
        var token = _jwtService.GenerateToken(userId, username, roles);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        Assert.Equal(userId, jwtToken.Claims.First(c => c.Type == JwtRegisteredClaimNames.Sub).Value);
        Assert.Equal(username, jwtToken.Claims.First(c => c.Type == JwtRegisteredClaimNames.UniqueName).Value);
        Assert.NotNull(jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti));
        
        var roleClaims = jwtToken.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();
        Assert.Equal(2, roleClaims.Count);
        Assert.Contains("Admin", roleClaims);
        Assert.Contains("User", roleClaims);
    }

    [Fact]
    public void GenerateToken_WithEmptyRoles_ShouldGenerateTokenWithoutRoleClaims()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var username = "testuser";
        var roles = new List<string>();

        // Act
        var token = _jwtService.GenerateToken(userId, username, roles);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        var roleClaims = jwtToken.Claims.Where(c => c.Type == ClaimTypes.Role).ToList();
        Assert.Empty(roleClaims);
    }

    [Fact]
    public void GenerateToken_WithCustomClaims_ShouldIncludeCustomClaims()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var username = "testuser";
        var roles = new List<string> { "User" };
        var customClaims = new List<Claim>
        {
            new Claim("email", "test@example.com"),
            new Claim("department", "Engineering"),
            new Claim("employee_id", "12345")
        };

        // Act
        var token = _jwtService.GenerateToken(userId, username, roles, customClaims);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        Assert.Equal("test@example.com", jwtToken.Claims.First(c => c.Type == "email").Value);
        Assert.Equal("Engineering", jwtToken.Claims.First(c => c.Type == "department").Value);
        Assert.Equal("12345", jwtToken.Claims.First(c => c.Type == "employee_id").Value);
    }

    [Fact]
    public void GenerateToken_WithNullCustomClaims_ShouldGenerateTokenSuccessfully()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var username = "testuser";
        var roles = new List<string> { "User" };

        // Act
        var token = _jwtService.GenerateToken(userId, username, roles);

        // Assert
        Assert.NotNull(token);
        Assert.NotEmpty(token);
    }

    [Fact]
    public void GenerateToken_WithEmptyCustomClaims_ShouldGenerateTokenSuccessfully()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var username = "testuser";
        var roles = new List<string> { "User" };
        var customClaims = new List<Claim>();

        // Act
        var token = _jwtService.GenerateToken(userId, username, roles, customClaims);

        // Assert
        Assert.NotNull(token);
        Assert.NotEmpty(token);
    }

    [Fact]
    public void GenerateToken_ShouldSetCorrectIssuerAndAudience()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var username = "testuser";
        var roles = new List<string> { "User" };

        // Act
        var token = _jwtService.GenerateToken(userId, username, roles);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        Assert.Equal("TestIssuer", jwtToken.Issuer);
        Assert.Contains("TestAudience", jwtToken.Audiences);
    }

    [Fact]
    public void GenerateToken_ShouldSetCorrectExpiration()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var username = "testuser";
        var roles = new List<string> { "User" };
        var beforeGeneration = DateTime.UtcNow;

        // Act
        var token = _jwtService.GenerateToken(userId, username, roles);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        
        // Token should be valid for approximately 60 minutes (allow 5 seconds tolerance)
        var expectedExpiration = beforeGeneration.AddMinutes(60);
        var timeDiff = (jwtToken.ValidTo - expectedExpiration).TotalSeconds;
        Assert.True(timeDiff is >= -1 and <= 5, 
            $"Expected expiration around {expectedExpiration}, but got {jwtToken.ValidTo}. Difference: {timeDiff} seconds");
    }

    [Fact]
    public void GenerateToken_ShouldUseRsaSha256Algorithm()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var username = "testuser";
        var roles = new List<string> { "User" };

        // Act
        var token = _jwtService.GenerateToken(userId, username, roles);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        Assert.Equal(SecurityAlgorithms.RsaSha256, jwtToken.SignatureAlgorithm);
    }

    [Fact]
    public void GenerateRefreshToken_ShouldReturnBase64String()
    {
        // Act
        var refreshToken = _jwtService.GenerateRefreshToken();

        // Assert
        Assert.NotNull(refreshToken);
        Assert.NotEmpty(refreshToken);
        
        // Verify it's a valid base64 string by attempting to decode
        var bytes = Convert.FromBase64String(refreshToken);
        Assert.Equal(64, bytes.Length);
    }

    [Fact]
    public void GenerateRefreshToken_ShouldGenerateUniqueTokens()
    {
        // Act
        var token1 = _jwtService.GenerateRefreshToken();
        var token2 = _jwtService.GenerateRefreshToken();
        var token3 = _jwtService.GenerateRefreshToken();

        // Assert
        Assert.NotEqual(token1, token2);
        Assert.NotEqual(token1, token3);
        Assert.NotEqual(token2, token3);
    }

    [Fact]
    public void GetPrincipalFromExpiredToken_WithValidToken_ShouldReturnClaimsPrincipal()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        const string username = "testuser";
        var roles = new List<string> { "Admin" };
        var token = _jwtService.GenerateToken(userId, username, roles);

        // Act
        var principal = _jwtService.GetPrincipalFromExpiredToken(token);
        
        // Assert
        Assert.NotNull(principal);
        Assert.Equal(userId, principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value);
        Assert.Equal(username, principal.FindFirst(JwtRegisteredClaimNames.UniqueName)?.Value);
    }

    [Fact]
    public void GetPrincipalFromExpiredToken_WithInvalidToken_ShouldReturnNull()
    {
        // Arrange
        var invalidToken = "invalid.token.string";

        // Act
        var principal = _jwtService.GetPrincipalFromExpiredToken(invalidToken);

        // Assert
        Assert.Null(principal);
    }

    [Fact]
    public void GetPrincipalFromExpiredToken_WithNullToken_ShouldReturnNull()
    {
        // Arrange
        string? nullToken = null;

        // Act
        var principal = _jwtService.GetPrincipalFromExpiredToken(nullToken!);

        // Assert
        Assert.Null(principal);
    }

    [Fact]
    public void GetPrincipalFromExpiredToken_WithEmptyToken_ShouldReturnNull()
    {
        // Arrange
        var emptyToken = string.Empty;

        // Act
        var principal = _jwtService.GetPrincipalFromExpiredToken(emptyToken);

        // Assert
        Assert.Null(principal);
    }

    [Fact]
    public void GetPrincipalFromExpiredToken_WithWrongIssuer_ShouldReturnNull()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var username = "testuser";
        var roles = new List<string> { "User" };
        var token = _jwtService.GenerateToken(userId, username, roles);

        // Change the issuer configuration
        var differentConfigMock = new Mock<IConfiguration>();
        differentConfigMock.Setup(c => c["Jwt:Issuer"]).Returns("DifferentIssuer");
        differentConfigMock.Setup(c => c["Jwt:Audience"]).Returns("TestAudience");
        differentConfigMock.Setup(c => c["Jwt:ExpirationMinutes"]).Returns("60");

        var differentService = new JwtService(differentConfigMock.Object, _keyServiceMock.Object);

        // Act - Try to validate with different issuer
        var principal = differentService.GetPrincipalFromExpiredToken(token);

        // Assert
        Assert.Null(principal);
    }

    [Fact]
    public void GetPrincipalFromExpiredToken_WithWrongSignature_ShouldReturnNull()
    {
        // Arrange - Create a token with different key
        using var differentRsa = System.Security.Cryptography.RSA.Create(2048);
        var differentPrivateKey = new RsaSecurityKey(differentRsa.ExportParameters(true));
        
        var differentKeyServiceMock = new Mock<IKeyManagementService>();
        differentKeyServiceMock.Setup(k => k.GetCurrentPrivateKey()).Returns(differentPrivateKey);
        
        var differentService = new JwtService(_configMock.Object, differentKeyServiceMock.Object);
        var token = differentService.GenerateToken("userId", "username", ["User"]);

        // Act - Try to validate with original service (different key)
        var principal = _jwtService.GetPrincipalFromExpiredToken(token);

        // Assert
        Assert.Null(principal);
    }

    [Fact]
    public void GenerateToken_ShouldCallKeyManagementService()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var username = "testuser";
        var roles = new List<string> { "User" };

        // Act
        _jwtService.GenerateToken(userId, username, roles);

        // Assert
        _keyServiceMock.Verify(k => k.GetCurrentPrivateKey(), Times.Once);
    }

    [Fact]
    public void GetPrincipalFromExpiredToken_ShouldCallKeyManagementService()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var username = "testuser";
        var roles = new List<string> { "User" };
        var token = _jwtService.GenerateToken(userId, username, roles);

        // Act
        _jwtService.GetPrincipalFromExpiredToken(token);

        // Assert
        _keyServiceMock.Verify(k => k.GetAllPublicKeys(), Times.Once);
    }

    [Theory]
    [InlineData("15")]
    [InlineData("30")]
    [InlineData("120")]
    public void GenerateToken_WithDifferentExpirationMinutes_ShouldSetCorrectExpiration(string expirationMinutes)
    {
        // Arrange
        _configMock.Setup(c => c["Jwt:ExpirationMinutes"]).Returns(expirationMinutes);
        var tempService = new JwtService(_configMock.Object, _keyServiceMock.Object);
        
        var userId = Guid.NewGuid().ToString();
        var username = "testuser";
        var roles = new List<string> { "User" };
        var beforeGeneration = DateTime.UtcNow;

        // Act
        var token = tempService.GenerateToken(userId, username, roles);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        
        var expectedExpiration = beforeGeneration.AddMinutes(int.Parse(expirationMinutes));
        var actualExpiration = jwtToken.ValidTo;
        
        // Allow 2 seconds tolerance for test execution time
        Assert.True(Math.Abs((actualExpiration - expectedExpiration).TotalSeconds) < 2);
    }

    [Fact]
    public void GenerateToken_WithMultipleRoles_ShouldIncludeAllRoles()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var username = "testuser";
        var roles = new List<string> { "Admin", "User", "Manager", "Developer" };

        // Act
        var token = _jwtService.GenerateToken(userId, username, roles);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        
        var roleClaims = jwtToken.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();
        Assert.Equal(4, roleClaims.Count);
        foreach (var role in roles)
        {
            Assert.Contains(role, roleClaims);
        }
    }

    [Fact]
    public void GenerateToken_WithCustomClaimsAndRoles_ShouldIncludeBoth()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var username = "testuser";
        var roles = new List<string> { "Admin", "User" };
        var customClaims = new List<Claim>
        {
            new Claim("email", "test@example.com"),
            new Claim("phone", "+1234567890")
        };

        // Act
        var token = _jwtService.GenerateToken(userId, username, roles, customClaims);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        
        // Verify roles
        var roleClaims = jwtToken.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();
        Assert.Equal(2, roleClaims.Count);
        Assert.Contains("Admin", roleClaims);
        Assert.Contains("User", roleClaims);
        
        // Verify custom claims
        Assert.Equal("test@example.com", jwtToken.Claims.First(c => c.Type == "email").Value);
        Assert.Equal("+1234567890", jwtToken.Claims.First(c => c.Type == "phone").Value);
    }
}
