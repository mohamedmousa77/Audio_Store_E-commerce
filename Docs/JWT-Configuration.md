# JWT Configuration & Security Setup

## ⚠️ IMPORTANT: Production Security

The JWT Secret in `appsettings.json` is for **DEVELOPMENT ONLY**. For production, use **User Secrets** or environment variables.

---

## 🔐 Setup User Secrets (Recommended)

### 1. Initialize User Secrets

```bash
cd AudioStore.Api
dotnet user-secrets init
```

### 2. Set JWT Secret

```bash
dotnet user-secrets set "JwtSettings:Secret" "YOUR-SUPER-SECRET-KEY-AT-LEAST-32-CHARACTERS-LONG"
```

### 3. Set Connection String (Optional)

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=YOUR_SERVER;Database=AudioStoreDB;User Id=YOUR_USER;Password=YOUR_PASSWORD;"
```

### 4. Verify Secrets

```bash
dotnet user-secrets list
```

---

## 📋 Current JWT Configuration

**Location:** `appsettings.json`

```json
{
  "JwtSettings": {
    "Secret": "Zt0SLbQkaqfkakMtElI5wsCd9cRyl94n!",  // ⚠️ CHANGE IN PRODUCTION
    "Issuer": "AudioStoreAPI",
    "Audience": "AudioStoreClient",
    "AccessTokenExpirationMinutes": 60,
    "RefreshTokenExpirationDays": 7
  }
}
```

---

## 🔑 JWT Features Implemented

### ✅ Access Tokens
- **Expiration:** 60 minutes (configurable)
- **Claims included:**
  - User ID (Sub, NameIdentifier)
  - Email
  - FirstName, LastName
  - Roles
- **Algorithm:** HMAC-SHA256

### ✅ Refresh Tokens
- **Expiration:** 7 days (configurable)
- **Storage:** Database (RefreshTokens table)
- **Security Features:**
  - Token rotation (old token revoked when refreshed)
  - IP address tracking (CreatedByIp, RevokedByIp)
  - Revocation support
  - Expiration tracking

### ✅ Token Validation
- Issuer validation
- Audience validation
- Lifetime validation
- Signature validation
- Zero clock skew

---

## 🚀 API Endpoints

### Login
```http
POST /api/auth/login
Content-Type: application/json

{
  "email": "admin@audiostore.com",
  "password": "Admin@123456"
}

Response:
{
  "userId": 1,
  "email": "admin@audiostore.com",
  "firstName": "System",
  "lastName": "Administrator",
  "roles": ["Administrator"],
  "token": {
    "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "base64-encoded-random-string",
    "expiresAt": "2026-01-18T04:00:00Z"
  }
}
```

### Refresh Token
```http
POST /api/auth/refresh-token
Content-Type: application/json

{
  "refreshToken": "base64-encoded-refresh-token"
}

Response:
{
  "accessToken": "new-jwt-token",
  "refreshToken": "new-refresh-token",
  "expiresAt": "2026-01-18T05:00:00Z"
}
```

### Revoke Token (Logout)
```http
POST /api/auth/revoke-token
Content-Type: application/json

{
  "refreshToken": "base64-encoded-refresh-token"
}

Response: 204 No Content
```

---

## 🔒 Using JWT in Requests

### Authorization Header

```http
GET /api/products
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

### Swagger UI

1. Click **"Authorize"** button
2. Enter: `Bearer YOUR_ACCESS_TOKEN`
3. Click **"Authorize"**
4. All requests will include the token

---

## 🛡️ Security Best Practices

### ✅ Implemented
- [x] Secure token generation (cryptographically random)
- [x] Token rotation on refresh
- [x] IP address tracking
- [x] Token revocation support
- [x] Short-lived access tokens (60 min)
- [x] Longer refresh tokens (7 days)
- [x] HTTPS enforcement (in production)

### 📝 Recommended for Production
- [ ] Move JWT Secret to Azure Key Vault / AWS Secrets Manager
- [ ] Implement rate limiting on auth endpoints
- [ ] Add CAPTCHA for login after failed attempts
- [ ] Monitor suspicious token usage patterns
- [ ] Implement token blacklist for immediate revocation
- [ ] Use Redis for refresh token storage (faster)
- [ ] Add 2FA (Two-Factor Authentication)

---

## 🧪 Testing JWT

### Using Postman

1. **Login:**
   - POST `https://localhost:7xxx/api/auth/login`
   - Body: `{ "email": "admin@audiostore.com", "password": "Admin@123456" }`
   - Save `accessToken` and `refreshToken`

2. **Use Protected Endpoint:**
   - GET `https://localhost:7xxx/api/products`
   - Headers: `Authorization: Bearer {accessToken}`

3. **Refresh Token:**
   - POST `https://localhost:7xxx/api/auth/refresh-token`
   - Body: `{ "refreshToken": "{refreshToken}" }`

### Using curl

```bash
# Login
curl -X POST https://localhost:7xxx/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@audiostore.com","password":"Admin@123456"}'

# Use token
curl -X GET https://localhost:7xxx/api/products \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN"
```

---

## 🔍 Troubleshooting

### "Unauthorized" Error
- Check token expiration
- Verify token format: `Bearer {token}`
- Ensure Issuer/Audience match configuration
- Check if user is active (`IsActive = true`)

### "Invalid Token" Error
- Token may be expired
- Token may have been revoked
- Secret key mismatch
- Token format incorrect

### Refresh Token Not Working
- Check if token exists in database
- Verify token is not expired (`ExpiresAt > DateTime.UtcNow`)
- Ensure token is not revoked (`IsRevoked = false`)
- Check IP address if implementing IP validation

---

## 📊 Database Schema

### RefreshTokens Table

```sql
SELECT 
    Id,
    UserId,
    Token,
    ExpiresAt,
    IsRevoked,
    RevokedAt,
    CreatedByIp,
    RevokedByIp,
    ReplacedByToken,
    CreatedAt
FROM RefreshTokens
WHERE UserId = 1
ORDER BY CreatedAt DESC;
```

### Clean Up Expired Tokens (Maintenance)

```sql
-- Delete expired and revoked tokens older than 30 days
DELETE FROM RefreshTokens
WHERE (IsRevoked = 1 OR ExpiresAt < GETUTCDATE())
  AND CreatedAt < DATEADD(DAY, -30, GETUTCDATE());
```
