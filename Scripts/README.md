# 📦 Database Seeding Scripts

## Automatic Seeding (DbInitializer)

The application automatically seeds the database on first run with:

### ✅ Roles
- **Administrator** - Full system access
- **Customer** - Regular user access
- **Guest** - Limited access

### ✅ Admin User
- **Email:** admin@audiostore.com
- **Password:** Admin@123456
- **Role:** Administrator

> ⚠️ **IMPORTANT:** Change the admin password after first login in production!

### ✅ Categories
1. **Headphones** - Over-ear and on-ear headphones
2. **Speakers** - Portable and home speakers
3. **Earphones** - In-ear earphones and wireless earbuds
4. **Wireless** - Wireless audio devices with Bluetooth

### ✅ Sample Products (6 products)
- XX99 Mark II Headphones ($2,999)
- XX99 Mark I Headphones ($1,750)
- XX59 Headphones ($899)
- ZX9 Speaker ($4,500)
- ZX7 Speaker ($3,500)
- YX1 Wireless Earphones ($599)

---

## Optional: Additional Products

To add more products to your database, run the SQL script:

```bash
# Using SQL Server Management Studio (SSMS)
1. Open SSMS
2. Connect to your database
3. Open: Scripts/additional-products.sql
4. Execute (F5)

# Using Azure Data Studio
1. Open Azure Data Studio
2. Connect to your database
3. Open: Scripts/additional-products.sql
4. Run (F5)

# Using sqlcmd
sqlcmd -S localhost -d AudioStore -i Scripts/additional-products.sql
```

This will add 4 additional products:
- Studio Pro Headphones ($1,299)
- Compact Bookshelf Speaker ($899)
- Sport Wireless Earbuds ($349)
- Audiophile Reference Speaker ($7,999)

---

## Verification

After running the application, verify the seeding:

```sql
-- Check Roles
SELECT * FROM Roles;

-- Check Admin User
SELECT Id, UserName, Email, FirstName, LastName FROM Users WHERE Email = 'admin@audiostore.com';

-- Check Categories
SELECT Id, Name, Slug FROM Categories WHERE IsDeleted = 0;

-- Check Products
SELECT Id, Name, Brand, Price, CategoryId FROM Products WHERE IsDeleted = 0;

-- Check Products with Category Names
SELECT 
    p.Id,
    p.Name,
    p.Brand,
    p.Price,
    c.Name AS CategoryName,
    p.StockQuantity,
    p.IsFeatured,
    p.IsNewProduct
FROM Products p
INNER JOIN Categories c ON p.CategoryId = c.Id
WHERE p.IsDeleted = 0
ORDER BY c.Name, p.Price DESC;
```

---

## Troubleshooting

### Database not seeding?
1. Check application logs in `logs/audiostore-*.txt`
2. Verify connection string in `appsettings.json`
3. Ensure migrations are applied: `dotnet ef database update`

### Admin user can't login?
- Email: `admin@audiostore.com`
- Password: `Admin@123456`
- Ensure email is confirmed (EmailConfirmed = true)

### Products not showing?
- Check `IsDeleted = 0` and `IsPublished = 1`
- Verify `StockQuantity > 0` and `IsAvailable = 1`
