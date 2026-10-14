using AutoMapper;
using FinalProjectAPI.Entities;
using FinalProjectAPI.Helpers.DTOs.Category;
using FinalProjectAPI.Helpers.DTOs.Order;
using FinalProjectAPI.Helpers.DTOs.OrderItem;
using FinalProjectAPI.Helpers.DTOs.Product;
using FinalProjectAPI.Helpers.DTOs.ProductImage;
using FinalProjectAPI.Helpers.Enums;

namespace FinalProjectAPI.Helpers.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<Category, CategoryDto>();
            CreateMap<CreateCategoryDto, Category>()
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Products, opt => opt.Ignore());

            CreateMap<UpdateCategoryDto, Category>()
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Products, opt => opt.Ignore());

            CreateMap<Product, ProductDto>()
                .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Category != null ? src.Category.Name : string.Empty))
                .ForMember(dest => dest.Images, opt => opt.MapFrom(src => src.ProductImages));

            CreateMap<CreateProductDto, Product>()
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Category, opt => opt.Ignore())
                .ForMember(dest => dest.ProductImages, opt => opt.Ignore());

            CreateMap<UpdateProductDto, Product>()
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Category, opt => opt.Ignore())
                .ForMember(dest => dest.ProductImages, opt => opt.Ignore());

            CreateMap<ProductImage, ProductImageDto>();
            
            CreateMap<CreateProductImageDto, ProductImage>()
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Product, opt => opt.Ignore());

            CreateMap<UpdateProductImageDto, ProductImage>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.ProductId, opt => opt.Ignore())
                .ForMember(dest => dest.Product, opt => opt.Ignore());

            CreateMap<Order, OrderDto>()
                .ForMember(dest => dest.StatusName, opt => opt.MapFrom(src => src.Status.ToString()))
                .ForMember(dest => dest.OrderItems, opt => opt.MapFrom(src => src.OrderItems));

            CreateMap<CreateOrderDto, Order>()
                .ForMember(dest => dest.OrderDate, opt => opt.MapFrom(_ => DateTime.UtcNow))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(_ => OrderStatus.Pending))
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.TotalAmount, opt => opt.Ignore())
                .ForMember(dest => dest.DeliveryDate, opt => opt.Ignore())
                .ForMember(dest => dest.OrderItems, opt => opt.Ignore());

            CreateMap<OrderItem, OrderItemDto>();
        }
    }
}
