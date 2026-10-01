using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using SportZone.Domain.Entities;

namespace SportZone.Application.Dtos
{

    public class CreateProductDto
    {
  
        public required string Name { get; set; } 
        public string? Description { get; set; }
        public string? Brand { get; set; }
        [Required]
        public decimal Price { get; set; }
        public string? PublicId { get; set; }
        public string? ImageUrl { get; set; } 
        [DefaultValue(1)]
        public int CategoryId { get; set; }
 
        public double? Discount { get; set; } = 0.0;
        public bool? IsNew { get; set; } = true;
        public string? Label { get; set; } = null; // e.g., "New Arrival", "Best Seller", to set banners on UI
        
        // Hair care & cosmetics
        public string? Volume { get; set; }
        public int Stock { get; set; } = 0;
        public string ProductType { get; set; } = "single"; // "single" | "combo"
        public decimal? CompareAtPrice { get; set; }
        public string? Benefits { get; set; }
        public string? Usage { get; set; }
        public string? Ingredients { get; set; }
        public string? AccentColor { get; set; }
        public List<ComboItemDto>? ComboItems { get; set; }

        public List<ProductSizeDto>? ProductSizes { get; set; }
        public List<CreateFeatureDto>? Features { get; set; }
    }

    public class ProductDto : CreateProductDto
    {
        
        public int Id { get; set; }
        [DefaultValue(false)]
        public bool IsDeleted { get; set; }
        public string? CategoryName { get; set; }
    }

    public class UpdateProductDto
    {
  
        public string? Name { get; set; } 
        public string? Description { get; set; }
        public string? Brand { get; set; }
      
        public decimal Price { get; set; }
        [JsonIgnore]
        public string? PublicId { get; set; }
        public string? ImageUrl { get; set; } 
        [DefaultValue(1)]
        public int? CategoryId { get; set; }
        public double? Discount { get; set; } = 0.0;
        public bool? IsNew { get; set; } = true;
        public string? Label { get; set; } = null; // e.g., "New Arrival", "Best Seller", to set banners on UI
        public string? Volume { get; set; }
        public int? Stock { get; set; }
        public string? ProductType { get; set; }
        public decimal? CompareAtPrice { get; set; }
        public string? Benefits { get; set; }
        public string? Usage { get; set; }
        public string? Ingredients { get; set; }
        public string? AccentColor { get; set; }
        public List<ProductSizeDto>? ProductSizes { get; set; }
        public List<ComboItemDto>? ComboItems { get; set; }
    }

     public class ProductInCartDto : CreateProductDto
    {
        
        public int Id { get; set; }
        [DefaultValue(false)]
        public bool IsDeleted { get; set; }
        public string? CategoryName { get; set; }
    }

    public class ProductInFeatureDto
    {
        public required int Id { get; set; } 
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? ImageUrl { get; set; } 
    }

    public class ComboItemDto
    {
        public int Id { get; set; }
        public int ComboProductId { get; set; }
        public int ComponentProductId { get; set; }
        public string? ComponentProductName { get; set; }
        public string? ComponentProductImageUrl { get; set; }
        public string? ComponentProductVolume { get; set; }
        public decimal ComponentProductPrice { get; set; }
        public int Quantity { get; set; } = 1;
    }

}