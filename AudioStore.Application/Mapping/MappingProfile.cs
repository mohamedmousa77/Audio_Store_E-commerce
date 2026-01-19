using AudioStore.Application.DTOs.Auth;
using AudioStore.Application.DTOs.Cart;
using AudioStore.Application.DTOs.Products;
using AudioStore.Domain.Entities;
using AutoMapper;
using CategoryDTO = AudioStore.Application.DTOs.Category.CategoryDTO;

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
            .ForMember(dest => dest.IsGuestCart,
                opt => opt.MapFrom(src => src.IsGuestCart));

        CreateMap<CartItem, CartItemDTO>()
            .ForMember(dest => dest.ProductName,
                opt => opt.MapFrom(src => src.Product.Name))
            .ForMember(dest => dest.ProductImage,
                opt => opt.MapFrom(src => src.Product.MainImage))
            .ForMember(dest => dest.AvailableStock,
                opt => opt.MapFrom(src => src.Product.StockQuantity));

        // ============ ORDER MAPPINGS (da completare) ============
        // CreateMap<Order, OrderDTO>()
        //     .ForMember(dest => dest.Items, opt => opt.MapFrom(src => src.OrderItems));

        // CreateMap<OrderItem, OrderItemDTO>()
        //     .ForMember(dest => dest.ProductName, opt => opt.MapFrom(src => src.Product.Name));
    }

}
