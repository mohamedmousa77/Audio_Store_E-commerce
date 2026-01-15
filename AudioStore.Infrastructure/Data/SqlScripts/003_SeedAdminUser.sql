DECLARE @AdminEmail NVARCHAR(256) = 'mousa@admin.test.com';

IF NOT EXISTS (SELECT 1 FROM Users WHERE Email = @AdminEmail)
BEGIN
    DECLARE @AdminUserId INT;
    DECLARE @AdminRoleId INT;

    -- Inserisci Admin User
    SET IDENTITY_INSERT Users ON;

    INSERT INTO Users (Id, FirstName, LastName, UserName, NormalizedUserName, Email, NormalizedEmail, 
                       EmailConfirmed, PasswordHash, SecurityStamp, PhoneNumber, RegistrationDate, IsActive, 
                       AccessFailedCount, LockoutEnabled, TwoFactorEnabled, PhoneNumberConfirmed)
    VALUES 
    (1, 'Admin', 'System', @AdminEmail, UPPER(@AdminEmail), @AdminEmail, UPPER(@AdminEmail), 
     1, 'AQAAAAIAAYagAAAAEJ1+8Z5vX8P8z3Q9K5F8L6H8K5F8L6H8K5F8L6H8K5F8L6H8K5F8L6A=', -- Password: Admin@123!
     NEWID(), NULL, GETUTCDATE(), 1, 0, 0, 0, 0);

    SET IDENTITY_INSERT Users OFF;

    SET @AdminUserId = 1;

    -- Recupera Role ID
    SELECT @AdminRoleId = Id FROM Roles WHERE NormalizedName = 'ADMINISTRATOR';

    -- Assegna ruolo Administrator
    INSERT INTO UserRoles (UserId, RoleId)
    VALUES (@AdminUserId, @AdminRoleId);
END
