using AutoMapper;
using SportZone.Application.Dtos;
using SportZone.Domain.Entities;

namespace SportZone.Application.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // --- 1. MAPPING CHO PRODUCT SIZE ---
            // Map 2 chiều giữa Entity và DTO chính
            CreateMap<ProductSize, ProductSizeDto>().ReverseMap();


            // --- 2. MAPPING CHO PRODUCT ---
            // Map hiển thị: Entity -> DTO
            CreateMap<Product, ProductDto>()
                .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Category.CategoryName))
                .ReverseMap();

            // Map tạo mới: Bỏ qua Id cha
            CreateMap<CreateProductDto, Product>()
                .ForMember(dest => dest.Id, opt => opt.Ignore());

            // Map cập nhật: Chỉ cập nhật các trường không null
            CreateMap<UpdateProductDto, Product>()
                // THÊM DÒNG NÀY ĐỂ CHẶN LỖI:
                .ForMember(dest => dest.ProductSizes, opt => opt.Ignore())

                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) =>
                    srcMember != null &&
                    (srcMember is not int i || i != 0) &&
                    (srcMember is not decimal d || d != 0)
                ));

            CreateMap<Product, ProductDto>()
                .ForMember(dest => dest.Features, opt => opt.MapFrom(src => src.Features))
                .ForMember(dest => dest.ComboItems, opt => opt.MapFrom(src => src.ComboItems));

            CreateMap<ComboItem, ComboItemDto>()
                .ForMember(dest => dest.ComponentProductName, opt => opt.MapFrom(src => src.ComponentProduct != null ? src.ComponentProduct.Name : null))
                .ForMember(dest => dest.ComponentProductImageUrl, opt => opt.MapFrom(src => src.ComponentProduct != null ? src.ComponentProduct.ImageUrl : null))
                .ForMember(dest => dest.ComponentProductVolume, opt => opt.MapFrom(src => src.ComponentProduct != null ? src.ComponentProduct.Volume : null))
                .ForMember(dest => dest.ComponentProductPrice, opt => opt.MapFrom(src => src.ComponentProduct != null ? src.ComponentProduct.Price : 0));

            CreateMap<Product, ProductInCartDto>().ReverseMap();
            CreateMap<Product, ProductInFeatureDto>().ReverseMap();

            // --- 3. MAPPING CHO CART ---
            CreateMap<CartItem, CartItemDto>()
                .ForMember(dest => dest.Product, opt => opt.MapFrom(src => src.Product))
                .ForMember(dest => dest.SizeName, opt => opt.MapFrom(src => src.ProductSize.SizeName))
                .AfterMap((src, dest) =>
                {
                    if (dest.SizeName != null)
                    {
                        dest.Quantity = src.Quantity;
                    }
                });

            CreateMap<Cart, CartDto>()
                .ForMember(dest => dest.Items, opt => opt.MapFrom(src => src.Items))
                .ReverseMap();

            CreateMap<AddCartItemDto, CartItem>();


            // --- 4. MAPPING CHO ORDER ---
            CreateMap<Order, OrderDto>().ReverseMap();
            CreateMap<Order, OrderDetailsDto>()
                .ForMember(dest => dest.Payment, opt => opt.MapFrom(src => src.Payment))
                .ReverseMap();

            CreateMap<OrderItem, OrderItemDto>()
                .ForMember(dest => dest.SizeName, opt => opt.MapFrom(src => src.ProductSize.SizeName))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Product.Name))
                .ForMember(dest => dest.ImageUrl, opt => opt.MapFrom(src => src.Product.ImageUrl))
                .ReverseMap();

            CreateMap<Payment, PaymentDto>().ReverseMap();


            // --- 5. CÁC THÀNH PHẦN KHÁC ---
            CreateMap<Category, CategoryDto>().ReverseMap();
            CreateMap<Promotion, PromotionDto>().ReverseMap();

            //-- FEATURE MAPPING---
            CreateMap<Feature, FeatureDto>().ReverseMap()
                .ForMember(dest => dest.Products, opt => opt.MapFrom(src => src.Products));
            CreateMap<Feature, CreateFeatureDto>().ReverseMap();
            CreateMap<Feature, UpdateFeatureDto>().ReverseMap();

            //-- APP USER MAPPING---
            CreateMap<AppUser, MemberDto>().ReverseMap()
            .ForMember(dest => dest.Orders, opt => opt.MapFrom(src => src.Orders));
        }
    }
}