using AudioStore.Common.Enums;
using AudioStore.Domain.Entities;
using Bogus;

namespace AudioStore.Tests.Helpers;

/// <summary>
/// Fluent API for building test entities with realistic data
/// </summary>
public class TestDataBuilder
{
    private static readonly Faker Faker = new();

    #region Product Builders

    public static ProductBuilder Product() => new();

    public class ProductBuilder
    {
        private int _id = 0;
        private string _name = Faker.Commerce.ProductName();
        private string? _slug;
        private string _description = Faker.Commerce.ProductDescription();
        private decimal _price = decimal.Parse(Faker.Commerce.Price(10, 1000));
        private int _categoryId = 1;
        private int _stockQuantity = Faker.Random.Int(0, 100);
        private string _mainImage = $"/images/{Faker.Random.AlphaNumeric(10)}.jpg";
        private string _brand = Faker.Company.CompanyName();
        private bool _isDeleted = false;

        public ProductBuilder WithId(int id)
        {
            _id = id;
            return this;
        }

        public ProductBuilder WithName(string name)
        {
            _name = name;
            return this;
        }

        public ProductBuilder WithSlug(string slug)
        {
            _slug = slug;
            return this;
        }

        public ProductBuilder WithDescription(string description)
        {
            _description = description;
            return this;
        }

        public ProductBuilder WithPrice(decimal price)
        {
            _price = price;
            return this;
        }

        public ProductBuilder WithCategoryId(int categoryId)
        {
            _categoryId = categoryId;
            return this;
        }

        public ProductBuilder WithStockQuantity(int quantity)
        {
            _stockQuantity = quantity;
            return this;
        }

        public ProductBuilder OutOfStock()
        {
            _stockQuantity = 0;
            return this;
        }

        public ProductBuilder InStock(int quantity = 10)
        {
            _stockQuantity = quantity;
            return this;
        }

        public ProductBuilder WithBrand(string brand)
        {
            _brand = brand;
            return this;
        }

        public ProductBuilder Deleted()
        {
            _isDeleted = true;
            return this;
        }

        public Product Build()
        {
            var slug = _slug ?? _name.ToLower().Replace(" ", "-");
            
            return new Product
            {
                Id = _id,
                Name = _name,
                Slug = slug,
                Description = _description,
                Price = _price,
                CategoryId = _categoryId,
                StockQuantity = _stockQuantity,
                MainImage = _mainImage,
                Brand = _brand,
                IsDeleted = _isDeleted,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = null
            };
        }
    }

    #endregion

    #region Category Builders

    public static CategoryBuilder Category() => new();

    public class CategoryBuilder
    {
        private int _id = 0;
        private string _name = Faker.Commerce.Categories(1)[0];
        private string? _slug;
        private string _imageUrl = $"/images/categories/{Faker.Random.AlphaNumeric(10)}.jpg";
        private bool _isDeleted = false;

        public CategoryBuilder WithId(int id)
        {
            _id = id;
            return this;
        }

        public CategoryBuilder WithName(string name)
        {
            _name = name;
            return this;
        }

        public CategoryBuilder WithSlug(string slug)
        {
            _slug = slug;
            return this;
        }

        public CategoryBuilder WithImageUrl(string imageUrl)
        {
            _imageUrl = imageUrl;
            return this;
        }

        public CategoryBuilder Deleted()
        {
            _isDeleted = true;
            return this;
        }

        public Category Build()
        {
            var slug = _slug ?? _name.ToLower().Replace(" ", "-");
            
            return new Category
            {
                Id = _id,
                Name = _name,
                Slug = slug,
                ImageUrl = _imageUrl,
                IsDeleted = _isDeleted,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = null
            };
        }
    }

    #endregion

    #region Order Builders

    public static OrderBuilder Order() => new();

    public class OrderBuilder
    {
        private int _id = 0;
        private int? _userId = null;
        private string _orderNumber = $"ORD-{Faker.Random.AlphaNumeric(10).ToUpper()}";
        private decimal _totalAmount = 0;
        private OrderStatus _status = OrderStatus.Pending;
        private string _shippingStreet = Faker.Address.StreetAddress();
        private string _shippingCity = Faker.Address.City();
        private string _shippingPostalCode = Faker.Address.ZipCode();
        private string _shippingCountry = Faker.Address.Country();
        private List<OrderItem> _orderItems = new();

        public OrderBuilder WithId(int id)
        {
            _id = id;
            return this;
        }

        public OrderBuilder WithUserId(int? userId)
        {
            _userId = userId;
            return this;
        }

        public OrderBuilder WithOrderNumber(string orderNumber)
        {
            _orderNumber = orderNumber;
            return this;
        }

        public OrderBuilder WithStatus(OrderStatus status)
        {
            _status = status;
            return this;
        }

        public OrderBuilder Pending() => WithStatus(OrderStatus.Pending);
        public OrderBuilder Processing() => WithStatus(OrderStatus.Processing);
        public OrderBuilder Shipped() => WithStatus(OrderStatus.Shipped);
        public OrderBuilder Delivered() => WithStatus(OrderStatus.Delivered);
        public OrderBuilder Cancelled() => WithStatus(OrderStatus.Cancelled);

        public OrderBuilder WithShippingAddress(string street, string city, string postalCode, string country)
        {
            _shippingStreet = street;
            _shippingCity = city;
            _shippingPostalCode = postalCode;
            _shippingCountry = country;
            return this;
        }

        public OrderBuilder WithOrderItem(OrderItem item)
        {
            _orderItems.Add(item);
            _totalAmount += item.Subtotal;
            return this;
        }

        public Order Build()
        {
            return new Order
            {
                Id = _id,
                UserId = _userId,
                OrderNumber = _orderNumber,
                TotalAmount = _totalAmount,
                Status = _status,
                ShippingStreet = _shippingStreet,
                ShippingCity = _shippingCity,
                ShippingPostalCode = _shippingPostalCode,
                ShippingCountry = _shippingCountry,
                OrderItems = _orderItems,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = null
            };
        }
    }

    #endregion

    #region OrderItem Builders

    public static OrderItemBuilder OrderItem() => new();

    public class OrderItemBuilder
    {
        private int _id = 0;
        private int _orderId = 0;
        private int _productId = 1;
        private int _quantity = 1;
        private decimal _unitPrice = 99.99m;

        public OrderItemBuilder WithId(int id)
        {
            _id = id;
            return this;
        }

        public OrderItemBuilder WithOrderId(int orderId)
        {
            _orderId = orderId;
            return this;
        }

        public OrderItemBuilder WithProductId(int productId)
        {
            _productId = productId;
            return this;
        }

        public OrderItemBuilder WithQuantity(int quantity)
        {
            _quantity = quantity;
            return this;
        }

        public OrderItemBuilder WithUnitPrice(decimal unitPrice)
        {
            _unitPrice = unitPrice;
            return this;
        }

        public OrderItem Build()
        {
            return new OrderItem
            {
                Id = _id,
                OrderId = _orderId,
                ProductId = _productId,
                Quantity = _quantity,
                UnitPrice = _unitPrice,
                Subtotal = _quantity * _unitPrice,
                CreatedAt = DateTime.UtcNow
            };
        }
    }

    #endregion

    #region CartItem Builders

    public static CartItemBuilder CartItem() => new();

    public class CartItemBuilder
    {
        private int _id = 0;
        private int _cartId = 1;
        private int _productId = 1;
        private int _quantity = 1;
        private decimal _unitPrice = 99.99m;

        public CartItemBuilder WithId(int id)
        {
            _id = id;
            return this;
        }

        public CartItemBuilder WithCartId(int cartId)
        {
            _cartId = cartId;
            return this;
        }

        public CartItemBuilder WithProductId(int productId)
        {
            _productId = productId;
            return this;
        }

        public CartItemBuilder WithQuantity(int quantity)
        {
            _quantity = quantity;
            return this;
        }

        public CartItemBuilder WithUnitPrice(decimal unitPrice)
        {
            _unitPrice = unitPrice;
            return this;
        }

        public CartItem Build()
        {
            return new CartItem
            {
                Id = _id,
                CartId = _cartId,
                ProductId = _productId,
                Quantity = _quantity,
                UnitPrice = _unitPrice,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = null
            };
        }
    }

    #endregion

    #region Predefined Scenarios

    /// <summary>
    /// Creates a valid product ready for testing
    /// </summary>
    public static Product ValidProduct() => Product().InStock(50).Build();

    /// <summary>
    /// Creates an out-of-stock product
    /// </summary>
    public static Product OutOfStockProduct() => Product().OutOfStock().Build();

    /// <summary>
    /// Creates a deleted product
    /// </summary>
    public static Product DeletedProduct() => Product().Deleted().Build();

    /// <summary>
    /// Creates a list of random products
    /// </summary>
    public static List<Product> RandomProducts(int count)
    {
        return Enumerable.Range(1, count)
            .Select(i => Product().WithId(i).Build())
            .ToList();
    }

    /// <summary>
    /// Creates a pending order with items
    /// </summary>
    public static Order PendingOrderWithItems(int? userId, int itemCount = 2)
    {
        var order = Order().WithUserId(userId).Pending();
        
        for (int i = 1; i <= itemCount; i++)
        {
            var item = OrderItem()
                .WithProductId(i)
                .WithQuantity(Faker.Random.Int(1, 5))
                .WithUnitPrice(decimal.Parse(Faker.Commerce.Price(10, 500)))
                .Build();
            
            order.WithOrderItem(item);
        }

        return order.Build();
    }

    #endregion
}
