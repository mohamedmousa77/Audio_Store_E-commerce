IF NOT EXISTS (SELECT 1 FROM Categories WHERE Slug = 'headphones')
BEGIN
    SET IDENTITY_INSERT Categories ON;

    INSERT INTO Categories (CategoryID, Name, Description, ImageUrl, Slug, CreatedAt, IsDeleted)
    VALUES 
    (1, 'Headphones', 'Premium over-ear and on-ear headphones', '/images/categories/headphones.png', 'headphones', GETUTCDATE(), 0),
    (2, 'Speakers', 'High-quality wireless and wired speakers', '/images/categories/speakers.png', 'speakers', GETUTCDATE(), 0),
    (3, 'Earphones', 'In-ear earphones and wireless earbuds', '/images/categories/earphones.png', 'earphones', GETUTCDATE(), 0);

    SET IDENTITY_INSERT Categories OFF;
END
