IF NOT EXISTS (SELECT 1 FROM Roles WHERE Name = 'Administrator')
BEGIN
    INSERT INTO Roles (Name, NormalizedName, ConcurrencyStamp)
    VALUES 
    ('Administrator', 'ADMINISTRATOR', NEWID()),
    ('Cliente', 'CLIENTE', NEWID());
END
