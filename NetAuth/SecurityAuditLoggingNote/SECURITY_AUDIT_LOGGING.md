# Refresh Token Security Audit Logging

## Overview
The `LoginWithRefreshToken` feature implements comprehensive security audit logging through Domain Events. All security-related events are automatically logged for monitoring, compliance, and threat detection.

## Security Events Logged

### 1. **Successful Token Rotation** ✅
**Event**: `RefreshTokenRotatedDomainEvent`  
**Log Level**: `Information`  
**Triggered When**: A refresh token is successfully rotated (normal operation)

```csharp
// Log Message
"Refresh token rotated successfully. UserId: {UserId}, OldTokenId: {OldTokenId}, NewTokenId: {NewTokenId}, DeviceId: {DeviceId}"
```

**Use Cases**:
- Track user activity patterns
- Verify token rotation frequency
- Compliance auditing

---

### 2. **Token Reuse Detection** 🚨
**Event**: `RefreshTokenReuseDetectedDomainEvent`  
**Log Level**: `Warning`  
**Triggered When**: An already-used (revoked/rotated) token is reused

```csharp
// Log Message
"SECURITY ALERT: Refresh token reuse detected! UserId: {UserId}, TokenId: {TokenId}, DeviceId: {DeviceId}, PreviousStatus: {PreviousStatus}"
```

**Security Implications**:
- **HIGH RISK** - Indicates potential token theft
- Automatically compromises entire token chain for user
- Requires immediate investigation

**Automated Response**:
1. Current token marked as `Compromised`
2. All active tokens for user are revoked
3. `RefreshTokenChainCompromisedDomainEvent` raised

---

### 3. **Device Mismatch Detection** 🚨
**Event**: `RefreshTokenDeviceMismatchDetectedDomainEvent`  
**Log Level**: `Warning`  
**Triggered When**: Token is used from a different device than originally bound

```csharp
// Log Message
"SECURITY ALERT: Device mismatch detected! UserId: {UserId}, TokenId: {TokenId}, ExpectedDeviceId: {ExpectedDeviceId}, ActualDeviceId: {ActualDeviceId}"
```

**Security Implications**:
- **HIGH RISK** - Indicates token theft or session hijacking
- Token is immediately marked as `Compromised`

**Automated Response**:
1. Token marked as `Compromised`
2. Access denied (401 Unauthorized)
3. User should re-authenticate

---

### 4. **Token Chain Compromised** 🔥
**Event**: `RefreshTokenChainCompromisedDomainEvent`  
**Log Level**: `Error`  
**Triggered When**: All refresh tokens for a user are compromised due to reuse detection

```csharp
// Log Message
"CRITICAL SECURITY ALERT: Entire refresh token chain compromised! UserId: {UserId}, CompromisedTokenCount: {CompromisedTokenCount}"
```

**Security Implications**:
- **CRITICAL** - User account potentially compromised
- All devices/sessions forcibly logged out
- Requires user re-authentication on all devices

**Recommended Actions**:
1. Alert security team
2. Notify user (email/SMS)
3. Consider temporary account lock
4. Force password change (optional)

---

### 5. **Expired Token Usage** ⚠️
**Event**: `RefreshTokenExpiredUsageDomainEvent`  
**Log Level**: `Warning`  
**Triggered When**: An expired token is submitted for refresh

```csharp
// Log Message
"Expired refresh token usage detected. UserId: {UserId}, TokenId: {TokenId}, ExpiredAt: {ExpiredAt}, AttemptedAt: {AttemptedAt}"
```

**Use Cases**:
- Normal user behavior (forgot to logout)
- Detect potential brute-force attempts
- Monitor token expiration patterns

---

## Implementation Architecture

### Domain Events (Domain Layer)
Located in: `/Domain/Users/DomainEvents/`

```
- RefreshTokenRotatedDomainEvent.cs
- RefreshTokenReuseDetectedDomainEvent.cs
- RefreshTokenDeviceMismatchDetectedDomainEvent.cs
- RefreshTokenChainCompromisedDomainEvent.cs
- RefreshTokenExpiredUsageDomainEvent.cs
```

### Domain Entity (Domain Layer)
`RefreshToken.cs` - Raises events via protected `AddDomainEvent()` method:

```csharp
public void MarkAsCompromisedDueToDeviceMismatch(DateTimeOffset revokedAt, string actualDeviceId)
{
    MarkAsCompromised(revokedAt);
    
    AddDomainEvent(new RefreshTokenDeviceMismatchDetectedDomainEvent(
        RefreshTokenId: Id,
        UserId: UserId,
        ExpectedDeviceId: DeviceId,
        ActualDeviceId: actualDeviceId));
}
```

### Event Handlers (Application Layer)
Located in: `/Application/Users/LoginWithRefreshToken/SecurityAuditEventHandlers.cs`

```
- RefreshTokenRotatedDomainEventHandler
- RefreshTokenReuseDetectedDomainEventHandler
- RefreshTokenDeviceMismatchDetectedDomainEventHandler
- RefreshTokenChainCompromisedDomainEventHandler
- RefreshTokenExpiredUsageDomainEventHandler
```

### Outbox Pattern Integration
Events are persisted to `outbox_messages` table and processed asynchronously via:
1. Domain events saved in same transaction as entity changes
2. `OutboxProcessor` polls and publishes events via MediatR
3. Event handlers execute (logging)
4. Processed events marked complete

---

## Monitoring & Alerting

### Log Levels by Severity

| Event | Level | Action Required |
|-------|-------|----------------|
| Token Rotation | Information | None |
| Expired Token | Warning | Monitor patterns |
| Device Mismatch | Warning | Investigate |
| Token Reuse | Warning | Immediate investigation |
| Chain Compromised | Error | Critical response |

### Recommended Alerts

1. **Multiple device mismatch attempts** (5+ in 1 hour)
   - Threshold-based alerting
   - Possible brute-force attack

2. **Token chain compromised**
   - Immediate notification to security team
   - Consider automatic account suspension

3. **High volume of expired token attempts**
   - May indicate credential stuffing
   - Review rate limiting configuration

---

## Query Log Examples

### Structured Logging (JSON)
```json
{
  "Timestamp": "2024-12-02T10:30:45Z",
  "Level": "Warning",
  "MessageTemplate": "SECURITY ALERT: Device mismatch detected!",
  "Properties": {
    "UserId": "550e8400-e29b-41d4-a716-446655440000",
    "TokenId": "660e8400-e29b-41d4-a716-446655440001",
    "ExpectedDeviceId": "device-abc-123",
    "ActualDeviceId": "device-xyz-789"
  }
}
```

### Search Queries (Splunk/ELK)

**Find all security alerts for a user:**
```
Level: (Warning OR Error) AND UserId: "550e8400-e29b-41d4-a716-446655440000"
```

**Detect brute-force patterns:**
```
MessageTemplate: "Device mismatch detected" 
| stats count by UserId 
| where count > 5
```

**Monitor token reuse attempts:**
```
MessageTemplate: "token reuse detected" 
| timechart span=1h count
```

---

## Testing Audit Logging

### Integration Tests
```csharp
[Fact]
public async Task Handle_TokenReuse_LogsSecurityAlert()
{
    // Arrange
    var token = RefreshToken.Create(...);
    token.MarkAsRevoked(utcNow); // Simulate previous rotation
    
    // Act
    var result = await handler.Handle(command, ct);
    
    // Assert
    result.IsLeft.Should().BeTrue();
    logger.VerifyLog(LogLevel.Warning, "token reuse detected");
}
```

### Manual Testing
1. Login and get refresh token
2. Use refresh token (creates new token)
3. Attempt to reuse old token → Should log "Token reuse detected"
4. Check logs for all 5 event types

---

## Compliance & Regulations

This audit logging supports compliance with:

- **GDPR**: User activity tracking and breach notification
- **PCI DSS**: Authentication and access logging requirements
- **SOC 2**: Security monitoring and incident response
- **HIPAA**: Audit trails for PHI access (if applicable)

---

## Future Enhancements

### Planned Features
1. **Persistent Audit Table** - Store security events in dedicated table
2. **Email Notifications** - Alert users of suspicious activity
3. **Admin Dashboard** - Real-time security event visualization
4. **Automated Responses** - Temporary account locks on repeated attacks
5. **GeoIP Validation** - Detect logins from unusual locations

### Extensibility
Add custom event handlers:
```csharp
internal sealed partial class CustomSecurityEventHandler 
    : INotificationHandler<RefreshTokenReuseDetectedDomainEvent>
{
    public async Task Handle(RefreshTokenReuseDetectedDomainEvent notification, CancellationToken ct)
    {
        // Send email to user
        await _emailService.SendSecurityAlertAsync(notification.UserId, ...);
        
        // Store in audit table
        await _auditRepository.LogSecurityEventAsync(...);
    }
}
```

---

## References

- Domain-Driven Design: Domain Events for cross-cutting concerns
- Outbox Pattern: Reliable event processing
- Refresh Token Rotation: OWASP recommendations
- Security Logging: OWASP Logging Cheat Sheet

