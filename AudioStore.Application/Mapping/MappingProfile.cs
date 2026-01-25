using AudioStore.Common.DTOs.Admin.CustomerManagement;
using AudioStore.Common.DTOs.Admin.Dashboard;
using AudioStore.Common.DTOs.Auth;
using AudioStore.Common.DTOs.Cart;
using AudioStore.Common.DTOs.Orders;
using AudioStore.Common.DTOs.Products;
using AudioStore.Common.DTOs.Profile;
using AudioStore.Domain.Entities;
using AudioStore.Domain.Interfaces;
using AutoMapper;
using CategoryDTO = AudioStore.Common.DTOs.Category.CategoryDTO;

namespace AudioStore.Application.Mapping;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // ============ USER MAPPINGS ============
        CreateMap<User, RegisterRequestDTO>().ReverseMap();

        // ============ PRODUCT MAPPINGS ============
        CreateMap<Product, ProductDTO>()
            .ForMember(dest => dest.CategoryName,
                opt => opt.MapFrom(src => src.Category.Name));

        CreateMap<CreateProductDTO, Product>();
        CreateMap<UpdateProductDTO, Product>();

        // ============ CATEGORY MAPPINGS ============
        CreateMap<Category, CategoryDTO>().ReverseMap();

        // ============ CART MAPPINGS ============
        CreateMap<Cart, CartDTO>()
            .ForMember(dest => dest.Items,
                opt => opt.MapFrom(src => src.CartItems))
            .ForMember(dest => dest.IsGuestCart,
                opt => opt.MapFrom(src => src.IsGuestCart))
            .ForMember(dest => dest.Subtotal,
                opt => opt.MapFrom(src => src.CartItems.Sum(i => i.Subtotal)))
            .ForMember(dest => dest.TotalItems,
                opt => opt.MapFrom(src => src.CartItems.Sum(i => i.Quantity)))
            .ForMember(dest => dest.ShippingCost,
                opt => opt.MapFrom((src, dest, destMember, context) =>
                    CalculateShippingCost(src.CartItems.Sum(i => i.Subtotal))))
            .ForMember(dest => dest.Tax,
                opt => opt.MapFrom((src, dest, destMember, context) =>
                    CalculateTax(src.CartItems.Sum(i => i.Subtotal))))
            .ForMember(dest => dest.TotalAmount,
                opt => opt.MapFrom((src, dest, destMember, context) =>
                {
                    var subtotal = src.CartItems.Sum(i => i.Subtotal);
                    var shipping = CalculateShippingCost(subtotal);
                    var tax = CalculateTax(subtotal);
                    return subtotal + shipping + tax;
                }));

        CreateMap<CartItem, CartItemDTO>()
            .ForMember(dest => dest.ProductName,
                opt => opt.MapFrom(src => src.Product.Name))
            .ForMember(dest => dest.ProductImage,
                opt => opt.MapFrom(src => src.Product.MainImage))
            .ForMember(dest => dest.Subtotal,
                opt => opt.MapFrom(src => src.Subtotal))
            .ForMember(dest => dest.AvailableStock,
                opt => opt.MapFrom(src => src.Product.StockQuantity));

        // ============ ORDER MAPPINGS ============
        CreateMap<Order, OrderDTO>()
            .ForMember(dest => dest.Items,
                opt => opt.MapFrom(src => src.OrderItems));

        CreateMap<OrderItem, OrderItemDTO>()
            .ForMember(dest => dest.ProductName,
                opt => opt.MapFrom(src => src.Product.Name))
            .ForMember(dest => dest.ProductImage,
                opt => opt.MapFrom(src => src.Product.MainImage))
            .ForMember(dest => dest.Subtotal,
                opt => opt.MapFrom(src => src.Subtotal));

        CreateMap<CreateOrderDTO, Order>();
        CreateMap<CreateOrderItemDTO, OrderItem>();

        // ============ ADDRESS MAPPINGS ============
        CreateMap<Address, AddressDTO>().ReverseMap();
        CreateMap<SaveAddressDTO, Address>();

        // ============ ADMIN DASHBOARD MAPPINGS ============
        CreateMap<TopProductData, TopProductDTO>();
        CreateMap<TopCategoryData, TopCategoryDTO>();

        // ============ USER/CUSTOMER MAPPINGS ============
        CreateMap<User, CustomerListItemDTO>()
            .ForMember(dest => dest.RegistrationDate, opt => opt.MapFrom(src => src.CreatedAt))
            .ForMember(dest => dest.TotalOrders, opt => opt.MapFrom(src => src.Orders.Count))
            .ForMember(dest => dest.LastOrderDate,
                opt => opt.MapFrom(src => src.Orders.Any() ? src.Orders.Max(o => o.OrderDate) : (DateTime?)null))
            .ForMember(dest => dest.TotalSpent, opt => opt.MapFrom(src => src.Orders.Sum(o => o.TotalAmount)));


    }

    //  Helper methods per calcoli (statici perché usati da AutoMapper)
    private static decimal CalculateShippingCost(decimal subtotal)
    {
        return subtotal >= 50 ? 0m : 5.00m;
    }

    private static decimal CalculateTax(decimal subtotal)
    {
        return subtotal * 0.22m; // IVA 22%
    }
}
