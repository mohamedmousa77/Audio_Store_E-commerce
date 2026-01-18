-- =============================================
-- AudioStore - Optional Additional Products
-- =============================================
-- This script adds extra products beyond the initial seed data
-- Execute this AFTER the application has run DbInitializer
-- =============================================

USE AudioStore;
GO

-- Verify categories exist
IF NOT EXISTS (SELECT 1 FROM Categories WHERE Slug = 'headphones')
BEGIN
    PRINT 'ERROR: Categories not found. Please run the application first to seed initial data.'
    RETURN;
END
GO

-- Get Category IDs
DECLARE @HeadphonesId INT = (SELECT Id FROM Categories WHERE Slug = 'headphones');
DECLARE @SpeakersId INT = (SELECT Id FROM Categories WHERE Slug = 'speakers');
DECLARE @EarphonesId INT = (SELECT Id FROM Categories WHERE Slug = 'earphones');
DECLARE @WirelessId INT = (SELECT Id FROM Categories WHERE Slug = 'wireless');

-- Additional Headphones
IF NOT EXISTS (SELECT 1 FROM Products WHERE Slug = 'studio-pro-headphones')
BEGIN
    INSERT INTO Products (
        Name, Brand, Description, Features, Price, CategoryId, 
        StockQuantity, MainImage, IsNewProduct, IsFeatured, 
        IsPublished, Slug, IsAvailable, CreatedAt
    )
    VALUES (
        'Studio Pro Headphones',
        'AudioStore',
        'Professional studio-grade headphones designed for audio engineers and music producers. Delivers exceptional clarity and precision across the entire frequency spectrum.',
        'Closed-back design for superior noise isolation. Detachable cable with gold-plated connectors. Memory foam ear cushions for extended comfort during long sessions.',
        1299.00,
        @HeadphonesId,
        25,
        '/images/products/studio-pro-headphones.jpg',
        0,
        0,
        1,
        'studio-pro-headphones',
        1,
        GETUTCDATE()
    );
    PRINT 'Added: Studio Pro Headphones';
END

-- Additional Speakers
IF NOT EXISTS (SELECT 1 FROM Products WHERE Slug = 'compact-bookshelf-speaker')
BEGIN
    INSERT INTO Products (
        Name, Brand, Description, Features, Price, CategoryId, 
        StockQuantity, MainImage, IsNewProduct, IsFeatured, 
        IsPublished, Slug, IsAvailable, CreatedAt
    )
    VALUES (
        'Compact Bookshelf Speaker',
        'AudioStore',
        'Perfect for smaller spaces without compromising on sound quality. These compact speakers deliver rich, room-filling audio with deep bass response.',
        'Dual 4-inch woofers and 1-inch silk dome tweeter. Rear-firing bass port for enhanced low-frequency response. Magnetic grille for clean aesthetics.',
        899.00,
        @SpeakersId,
        18,
        '/images/products/compact-bookshelf-speaker.jpg',
        0,
        0,
        1,
        'compact-bookshelf-speaker',
        1,
        GETUTCDATE()
    );
    PRINT 'Added: Compact Bookshelf Speaker';
END

-- Additional Wireless Earphones
IF NOT EXISTS (SELECT 1 FROM Products WHERE Slug = 'sport-wireless-earbuds')
BEGIN
    INSERT INTO Products (
        Name, Brand, Description, Features, Price, CategoryId, 
        StockQuantity, MainImage, IsNewProduct, IsFeatured, 
        IsPublished, Slug, IsAvailable, CreatedAt
    )
    VALUES (
        'Sport Wireless Earbuds',
        'AudioStore',
        'Designed for active lifestyles. These sweat-resistant earbuds stay secure during intense workouts while delivering powerful, motivating sound.',
        'IPX7 water resistance. Secure-fit ear hooks. 8-hour battery life with charging case providing 24 hours total. Touch controls for easy operation.',
        349.00,
        @WirelessId,
        40,
        '/images/products/sport-wireless-earbuds.jpg',
        1,
        0,
        1,
        'sport-wireless-earbuds',
        1,
        GETUTCDATE()
    );
    PRINT 'Added: Sport Wireless Earbuds';
END

-- Additional Premium Product
IF NOT EXISTS (SELECT 1 FROM Products WHERE Slug = 'audiophile-reference-speaker')
BEGIN
    INSERT INTO Products (
        Name, Brand, Description, Features, Price, CategoryId, 
        StockQuantity, MainImage, IsNewProduct, IsFeatured, 
        IsPublished, Slug, IsAvailable, CreatedAt
    )
    VALUES (
        'Audiophile Reference Speaker',
        'AudioStore',
        'The ultimate reference speaker for the most discerning audiophiles. Hand-crafted with premium materials and cutting-edge acoustic engineering.',
        'Three-way design with dedicated woofer, midrange, and tweeter. Solid hardwood cabinet with internal bracing. Bi-wire/bi-amp capable terminals.',
        7999.00,
        @SpeakersId,
        5,
        '/images/products/audiophile-reference-speaker.jpg',
        1,
        1,
        1,
        'audiophile-reference-speaker',
        1,
        GETUTCDATE()
    );
    PRINT 'Added: Audiophile Reference Speaker';
END

PRINT '';
PRINT '✅ Additional products added successfully!';
PRINT 'Total products in database: ' + CAST((SELECT COUNT(*) FROM Products WHERE IsDeleted = 0) AS VARCHAR);
GO
