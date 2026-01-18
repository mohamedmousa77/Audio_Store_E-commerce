using AudioStore.Application.DTOs.Auth;
using AudioStore.Application.DTOs.Category;
using AudioStore.Application.DTOs.Products;
using AudioStore.Domain.Entities;
using AutoMapper;
using System;
using System.Collections.Generic;
using System.Text;

namespace AudioStore.Application.Mapping;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // User Mappings
        CreateMap<User, RegisterRequestDTO>().ReverseMap();

        // Product Mappings
        CreateMap<Product, ProductDTO>()
            .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Category.Name));

        CreateMap<CreateProductDTO, Product>();

        // Category Mappings
        CreateMap<Category, CategoryDTO>();

        // Order Mappings
        //CreateMap<Order, OrderDTO>()
        //    .ForMember(dest => dest.Items, opt => opt.MapFrom(src => src.OrderItems));

        //CreateMap<OrderItem, OrderItemDTO>()
        //    .ForMember(dest => dest.ProductName, opt => opt.MapFrom(src => src.Product.Name));

        // Cart Mappings
        //CreateMap<Cart, CartDTO>();

        //CreateMap<CartItem, CartItemDTO>()
        //    .ForMember(dest => dest.ProductName, opt => opt.MapFrom(src => src.Product.Name));
    }

}
