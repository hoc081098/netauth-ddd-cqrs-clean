using NetAuth.Domain.Users;
using NetAuth.Domain.Users.DomainEvents;

// ReSharper disable ParameterOnlyUsedForPreconditionCheck.Local

namespace NetAuth.UnitTests.Domain.Users;

public static class RefreshTokenTestData
{
    public static readonly Guid UserId = Guid.NewGuid();
    public const string TokenHash = "hashed_token_value_12345";
    public const string DeviceId = "device-12345-abcde";

    public static readonly DateTimeOffset CurrentUtc =
        new(year: 2025, month: 1, day: 1, hour: 12, minute: 0, second: 0, offset: TimeSpan.Zero);

    public static readonly DateTimeOffset FutureExpiration =
        new(year: 2025, month: 1, day: 8, hour: 12, minute: 0, second: 0, offset: TimeSpan.Zero);

    public static readonly DateTimeOffset PastExpiration =
        new(year: 2024, month: 12, day: 25, hour: 12, minute: 0, second: 0, offset: TimeSpan.Zero);

    public static RefreshToken CreateRefreshToken(
        string? tokenHash = null,
        DateTimeOffset? expiresOnUtc = null,
        Guid? userId = null,
        string? deviceId = null) =>
        RefreshToken.Create(
            tokenHash: tokenHash ?? TokenHash,
            expiresOnUtc: expiresOnUtc ?? FutureExpiration,
            userId: userId ?? UserId,
            deviceId: deviceId ?? DeviceId);
}

public class RefreshTokenTests : BaseTest
{
    [Fact]
    public void Create_WithValidData_ShouldReturnSuccess()
    {
        // Arrange
        var tokenHash = RefreshTokenTestData.TokenHash;
        var expiresOnUtc = RefreshTokenTestData.FutureExpiration;
        var userId = RefreshTokenTestData.UserId;
        var deviceId = RefreshTokenTestData.DeviceId;

        // Act
        var refreshToken = RefreshToken.Create(
            tokenHash: tokenHash,
            expiresOnUtc: expiresOnUtc,
            userId: userId,
            deviceId: deviceId);

        // Assert
        Assert.NotNull(refreshToken);
        Assert.NotEqual(Guid.Empty, refreshToken.Id);
        Assert.Equal(tokenHash, refreshToken.TokenHash);
        Assert.Equal(expiresOnUtc, refreshToken.ExpiresOnUtc);
        Assert.Equal(userId, refreshToken.UserId);
        Assert.Equal(deviceId, refreshToken.DeviceId);
        Assert.Equal(RefreshTokenStatus.Active, refreshToken.Status);
        Assert.Null(refreshToken.RevokedAt);
        Assert.Null(refreshToken.ReplacedById);

        var domainEvent = AssertDomainEventWasPublished<RefreshTokenCreatedDomainEvent>(refreshToken);
        Assert.Equal(refreshToken.Id, domainEvent.RefreshTokenId);
        Assert.Equal(userId, domainEvent.UserId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    public void Create_WithInvalidTokenHash_ShouldThrowArgumentException(string? invalidTokenHash)
    {
        // Arrange
        var expiresOnUtc = RefreshTokenTestData.FutureExpiration;
        var userId = RefreshTokenTestData.UserId;
        var deviceId = RefreshTokenTestData.DeviceId;

        // Act & Assert
        Assert.ThrowsAny<ArgumentException>(() =>
            RefreshToken.Create(
                tokenHash: invalidTokenHash!,
                expiresOnUtc: expiresOnUtc,
                userId: userId,
                deviceId: deviceId));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    public void Create_WithInvalidDeviceId_ShouldThrowArgumentException(string? invalidDeviceId)
    {
        // Arrange
        var tokenHash = RefreshTokenTestData.TokenHash;
        var expiresOnUtc = RefreshTokenTestData.FutureExpiration;
        var userId = RefreshTokenTestData.UserId;

        // Act & Assert
        Assert.ThrowsAny<ArgumentException>(() =>
            RefreshToken.Create(
                tokenHash: tokenHash,
                expiresOnUtc: expiresOnUtc,
                userId: userId,
                deviceId: invalidDeviceId!));
    }

    [Fact]
    public void Create_WithDefaultUserId_ShouldThrowArgumentException()
    {
        // Arrange
        var tokenHash = RefreshTokenTestData.TokenHash;
        var expiresOnUtc = RefreshTokenTestData.FutureExpiration;
        var userId = Guid.Empty;
        var deviceId = RefreshTokenTestData.DeviceId;

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            RefreshToken.Create(
                tokenHash: tokenHash,
                expiresOnUtc: expiresOnUtc,
                userId: userId,
                deviceId: deviceId));
    }

    [Fact]
    public void Create_WithDefaultExpiresOnUtc_ShouldThrowArgumentException()
    {
        // Arrange
        var tokenHash = RefreshTokenTestData.TokenHash;
        var expiresOnUtc = default(DateTimeOffset);
        var userId = RefreshTokenTestData.UserId;
        var deviceId = RefreshTokenTestData.DeviceId;

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            RefreshToken.Create(
                tokenHash: tokenHash,
                expiresOnUtc: expiresOnUtc,
                userId: userId,
                deviceId: deviceId));
    }

    [Fact]
    public void IsExpired_WithCurrentTimeBeforeExpiration_ShouldReturnFalse()
    {
        // Arrange
        var refreshToken = RefreshTokenTestData.CreateRefreshToken();
        var currentUtc = RefreshTokenTestData.CurrentUtc;

        // Act
        var isExpired = refreshToken.IsExpired(currentUtc);

        // Assert
        Assert.False(isExpired);
    }

    [Fact]
    public void IsExpired_WithCurrentTimeAfterExpiration_ShouldReturnTrue()
    {
        // Arrange
        var refreshToken = RefreshTokenTestData.CreateRefreshToken();
        var currentUtc = RefreshTokenTestData.FutureExpiration.AddDays(1);

        // Act
        var isExpired = refreshToken.IsExpired(currentUtc);

        // Assert
        Assert.True(isExpired);
    }

    [Fact]
    public void IsExpired_WithCurrentTimeEqualToExpiration_ShouldReturnTrue()
    {
        // Arrange
        var refreshToken = RefreshTokenTestData.CreateRefreshToken();
        var currentUtc = RefreshTokenTestData.FutureExpiration;

        // Act
        var isExpired = refreshToken.IsExpired(currentUtc);

        // Assert
        Assert.True(isExpired);
    }

    [Fact]
    public void IsValid_WithActiveAndNotExpired_ShouldReturnTrue()
    {
        // Arrange
        var refreshToken = RefreshTokenTestData.CreateRefreshToken();
        var currentUtc = RefreshTokenTestData.CurrentUtc;

        // Act
        var isValid = refreshToken.IsValid(currentUtc);

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void IsValid_WithActiveButExpired_ShouldReturnFalse()
    {
        // Arrange
        var refreshToken = RefreshTokenTestData.CreateRefreshToken();
        var currentUtc = RefreshTokenTestData.FutureExpiration.AddDays(1);

        // Act
        var isValid = refreshToken.IsValid(currentUtc);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void IsValid_WithRevokedStatus_ShouldReturnFalse()
    {
        // Arrange
        var refreshToken = RefreshTokenTestData.CreateRefreshToken();
        var revokedAt = RefreshTokenTestData.CurrentUtc;
        refreshToken.MarkAsRevokedDueToExpiration(revokedAt);
        var currentUtc = RefreshTokenTestData.CurrentUtc;

        // Act
        var isValid = refreshToken.IsValid(currentUtc);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void IsValid_WithCompromisedStatus_ShouldReturnFalse()
    {
        // Arrange
        var refreshToken = RefreshTokenTestData.CreateRefreshToken();
        var detectedAt = RefreshTokenTestData.CurrentUtc;
        refreshToken.MarkAsCompromisedDueToReuse(detectedAt, chainAffected: false);
        var currentUtc = RefreshTokenTestData.CurrentUtc;

        // Act
        var isValid = refreshToken.IsValid(currentUtc);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void MarkAsRevokedDueToExpiration_ShouldUpdateStatusAndRaiseEvent()
    {
        // Arrange
        var refreshToken = RefreshTokenTestData.CreateRefreshToken();
        var revokedAt = RefreshTokenTestData.CurrentUtc;

        // Act
        refreshToken.MarkAsRevokedDueToExpiration(revokedAt);

        // Assert
        Assert.Equal(RefreshTokenStatus.Revoked, refreshToken.Status);
        Assert.Equal(revokedAt, refreshToken.RevokedAt);

        var domainEvent = AssertDomainEventWasPublished<RefreshTokenExpiredUsageDomainEvent>(refreshToken);
        Assert.Equal(refreshToken.Id, domainEvent.RefreshTokenId);
        Assert.Equal(refreshToken.UserId, domainEvent.UserId);
        Assert.Equal(refreshToken.ExpiresOnUtc, domainEvent.ExpiresOnUtc);
        Assert.Equal(revokedAt, domainEvent.AttemptedAt);
    }

    [Fact]
    public void MarkAsRevokedDueToExpiration_WhenAlreadyRevoked_ShouldNotChangeState()
    {
        // Arrange
        var refreshToken = RefreshTokenTestData.CreateRefreshToken();
        var firstRevokedAt = RefreshTokenTestData.CurrentUtc;
        refreshToken.MarkAsRevokedDueToExpiration(firstRevokedAt);
        var secondRevokedAt = RefreshTokenTestData.CurrentUtc.AddHours(1);

        // Act
        refreshToken.MarkAsRevokedDueToExpiration(secondRevokedAt);

        // Assert
        Assert.Equal(RefreshTokenStatus.Revoked, refreshToken.Status);
        Assert.Equal(firstRevokedAt, refreshToken.RevokedAt); // Should remain the first revoked time
    }

    [Fact]
    public void MarkAsCompromisedDueToDeviceMismatch_ShouldUpdateStatusAndRaiseEvent()
    {
        // Arrange
        var refreshToken = RefreshTokenTestData.CreateRefreshToken();
        var revokedAt = RefreshTokenTestData.CurrentUtc;
        var actualDeviceId = "different-device-id";

        // Act
        refreshToken.MarkAsCompromisedDueToDeviceMismatch(revokedAt, actualDeviceId);

        // Assert
        Assert.Equal(RefreshTokenStatus.Compromised, refreshToken.Status);
        Assert.Equal(revokedAt, refreshToken.RevokedAt);

        var domainEvent = AssertDomainEventWasPublished<RefreshTokenDeviceMismatchDetectedDomainEvent>(refreshToken);
        Assert.Equal(refreshToken.Id, domainEvent.RefreshTokenId);
        Assert.Equal(refreshToken.UserId, domainEvent.UserId);
        Assert.Equal(RefreshTokenTestData.DeviceId, domainEvent.ExpectedDeviceId);
        Assert.Equal(actualDeviceId, domainEvent.ActualDeviceId);
    }

    [Fact]
    public void MarkAsCompromisedDueToReuse_WithoutChainAffected_ShouldUpdateStatusAndRaiseOneEvent()
    {
        // Arrange
        var refreshToken = RefreshTokenTestData.CreateRefreshToken();
        var detectedAt = RefreshTokenTestData.CurrentUtc;

        // Act
        refreshToken.MarkAsCompromisedDueToReuse(detectedAt, chainAffected: false);

        // Assert
        Assert.Equal(RefreshTokenStatus.Compromised, refreshToken.Status);
        Assert.Equal(detectedAt, refreshToken.RevokedAt);

        var reuseEvent = AssertDomainEventWasPublished<RefreshTokenReuseDetectedDomainEvent>(refreshToken);
        Assert.Equal(refreshToken.Id, reuseEvent.RefreshTokenId);
        Assert.Equal(refreshToken.UserId, reuseEvent.UserId);
        Assert.Equal(RefreshTokenTestData.DeviceId, reuseEvent.DeviceId);
        Assert.Equal(RefreshTokenStatus.Active, reuseEvent.PreviousStatus);

        // Should NOT raise chain compromised event
        var chainEvents = refreshToken.DomainEvents.OfType<RefreshTokenChainCompromisedDomainEvent>();
        Assert.Empty(chainEvents);
    }

    [Fact]
    public void MarkAsCompromisedDueToReuse_WithChainAffected_ShouldUpdateStatusAndRaiseTwoEvents()
    {
        // Arrange
        var refreshToken = RefreshTokenTestData.CreateRefreshToken();
        var detectedAt = RefreshTokenTestData.CurrentUtc;

        // Act
        refreshToken.MarkAsCompromisedDueToReuse(detectedAt, chainAffected: true);

        // Assert
        Assert.Equal(RefreshTokenStatus.Compromised, refreshToken.Status);
        Assert.Equal(detectedAt, refreshToken.RevokedAt);

        var reuseEvent = AssertDomainEventWasPublished<RefreshTokenReuseDetectedDomainEvent>(refreshToken);
        Assert.Equal(refreshToken.Id, reuseEvent.RefreshTokenId);
        Assert.Equal(refreshToken.UserId, reuseEvent.UserId);

        var chainEvent = AssertDomainEventWasPublished<RefreshTokenChainCompromisedDomainEvent>(refreshToken);
        Assert.Equal(refreshToken.UserId, chainEvent.UserId);
    }

    [Fact]
    public void MarkAsCompromisedDueToReuse_WhenAlreadyCompromised_ShouldNotChangeState()
    {
        // Arrange
        var refreshToken = RefreshTokenTestData.CreateRefreshToken();
        var firstDetectedAt = RefreshTokenTestData.CurrentUtc;
        refreshToken.MarkAsCompromisedDueToReuse(firstDetectedAt, chainAffected: false);
        var secondDetectedAt = RefreshTokenTestData.CurrentUtc.AddHours(1);

        // Act
        refreshToken.MarkAsCompromisedDueToReuse(secondDetectedAt, chainAffected: false);

        // Assert
        Assert.Equal(RefreshTokenStatus.Compromised, refreshToken.Status);
        Assert.Equal(firstDetectedAt, refreshToken.RevokedAt); // Should remain the first compromised time
    }

    [Fact]
    public void Rotate_ShouldCreateNewTokenAndRevokeCurrentOne()
    {
        // Arrange
        var refreshToken = RefreshTokenTestData.CreateRefreshToken();
        var newTokenHash = "new_hashed_token_value";
        var newExpiresOnUtc = RefreshTokenTestData.FutureExpiration.AddDays(7);
        var revokedAt = RefreshTokenTestData.CurrentUtc;

        // Act
        var newRefreshToken = refreshToken.Rotate(newTokenHash, newExpiresOnUtc, revokedAt);

        // Assert - Old token should be revoked
        Assert.Equal(RefreshTokenStatus.Revoked, refreshToken.Status);
        Assert.Equal(revokedAt, refreshToken.RevokedAt);
        Assert.Equal(newRefreshToken.Id, refreshToken.ReplacedById);

        // Assert - New token should be active
        Assert.NotNull(newRefreshToken);
        Assert.NotEqual(Guid.Empty, newRefreshToken.Id);
        Assert.Equal(newTokenHash, newRefreshToken.TokenHash);
        Assert.Equal(newExpiresOnUtc, newRefreshToken.ExpiresOnUtc);
        Assert.Equal(refreshToken.UserId, newRefreshToken.UserId);
        Assert.Equal(RefreshTokenTestData.DeviceId, newRefreshToken.DeviceId);
        Assert.Equal(RefreshTokenStatus.Active, newRefreshToken.Status);
        Assert.Null(newRefreshToken.RevokedAt);
        Assert.Null(newRefreshToken.ReplacedById);

        // Assert - Domain events
        var rotatedEvent = AssertDomainEventWasPublished<RefreshTokenRotatedDomainEvent>(refreshToken);
        Assert.Equal(refreshToken.Id, rotatedEvent.OldRefreshTokenId);
        Assert.Equal(newRefreshToken.Id, rotatedEvent.NewRefreshTokenId);
        Assert.Equal(refreshToken.UserId, rotatedEvent.UserId);
        Assert.Equal(RefreshTokenTestData.DeviceId, rotatedEvent.DeviceId);

        // New token should have its own created event
        var createdEvent = AssertDomainEventWasPublished<RefreshTokenCreatedDomainEvent>(newRefreshToken);
        Assert.Equal(newRefreshToken.Id, createdEvent.RefreshTokenId);
        Assert.Equal(refreshToken.UserId, createdEvent.UserId);
    }

    [Fact]
    public void Rotate_MultipleTimesSequentially_ShouldMaintainChain()
    {
        // Arrange
        var firstToken = RefreshTokenTestData.CreateRefreshToken();
        var revokedAt = RefreshTokenTestData.CurrentUtc;

        // Act - First rotation
        var secondToken = firstToken.Rotate(
            newTokenHash: "second_token_hash",
            newExpiresOnUtc: RefreshTokenTestData.FutureExpiration.AddDays(7),
            revokedAt: revokedAt);

        // Act - Second rotation
        var thirdToken = secondToken.Rotate(
            newTokenHash: "third_token_hash",
            newExpiresOnUtc: RefreshTokenTestData.FutureExpiration.AddDays(14),
            revokedAt: revokedAt.AddHours(1));

        // Assert - Chain integrity
        Assert.Equal(secondToken.Id, firstToken.ReplacedById);
        Assert.Equal(thirdToken.Id, secondToken.ReplacedById);
        Assert.Null(thirdToken.ReplacedById);

        // Assert - All old tokens should be revoked
        Assert.Equal(RefreshTokenStatus.Revoked, firstToken.Status);
        Assert.Equal(RefreshTokenStatus.Revoked, secondToken.Status);
        Assert.Equal(RefreshTokenStatus.Active, thirdToken.Status);
    }

    [Fact]
    public void RefreshTokenExpressions_IsExpired_ShouldFilterCorrectly()
    {
        // Arrange
        var userId = RefreshTokenTestData.UserId;
        var currentUtc = RefreshTokenTestData.CurrentUtc;

        var activeToken = RefreshToken.Create(
            tokenHash: "active_token",
            expiresOnUtc: RefreshTokenTestData.FutureExpiration,
            userId: userId,
            deviceId: RefreshTokenTestData.DeviceId);

        var expiredToken = RefreshToken.Create(
            tokenHash: "expired_token",
            expiresOnUtc: RefreshTokenTestData.PastExpiration,
            userId: userId,
            deviceId: RefreshTokenTestData.DeviceId);

        var tokens = new[] { activeToken, expiredToken };

        // Act
        var expression = RefreshTokenExpressions.IsExpired(userId, currentUtc);
        var compiledExpression = expression.Compile();
        var expiredTokens = tokens.Where(compiledExpression).ToList();

        // Assert
        Assert.Single(expiredTokens);
        Assert.Contains(expiredToken, expiredTokens);
    }

    [Fact]
    public void RefreshTokenExpressions_IsValid_ShouldFilterCorrectly()
    {
        // Arrange
        var userId = RefreshTokenTestData.UserId;
        var currentUtc = RefreshTokenTestData.CurrentUtc;

        var activeToken = RefreshToken.Create(
            tokenHash: "active_token",
            expiresOnUtc: RefreshTokenTestData.FutureExpiration,
            userId: userId,
            deviceId: RefreshTokenTestData.DeviceId);

        var expiredToken = RefreshToken.Create(
            tokenHash: "expired_token",
            expiresOnUtc: RefreshTokenTestData.PastExpiration,
            userId: userId,
            deviceId: RefreshTokenTestData.DeviceId);

        var revokedToken = RefreshToken.Create(
            tokenHash: "revoked_token",
            expiresOnUtc: RefreshTokenTestData.FutureExpiration,
            userId: userId,
            deviceId: RefreshTokenTestData.DeviceId);
        revokedToken.MarkAsRevokedDueToExpiration(currentUtc);

        var tokens = new[] { activeToken, expiredToken, revokedToken };

        // Act
        var expression = RefreshTokenExpressions.IsValid(userId, currentUtc);
        var compiledExpression = expression.Compile();
        var validTokens = tokens.Where(compiledExpression).ToList();

        // Assert
        Assert.Single(validTokens);
        Assert.Contains(activeToken, validTokens);
    }

    [Fact]
    public void RefreshTokenExpressions_IsValid_WithDifferentUserId_ShouldNotMatch()
    {
        // Arrange
        var userId = RefreshTokenTestData.UserId;
        var differentUserId = Guid.NewGuid();
        var currentUtc = RefreshTokenTestData.CurrentUtc;

        var token = RefreshToken.Create(
            tokenHash: "token",
            expiresOnUtc: RefreshTokenTestData.FutureExpiration,
            userId: userId,
            deviceId: RefreshTokenTestData.DeviceId);

        var tokens = new[] { token };

        // Act
        var expression = RefreshTokenExpressions.IsValid(differentUserId, currentUtc);
        var compiledExpression = expression.Compile();
        var validTokens = tokens.Where(compiledExpression).ToList();

        // Assert
        Assert.Empty(validTokens);
    }
}