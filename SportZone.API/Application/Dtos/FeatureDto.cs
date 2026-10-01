using System.Text.Json.Serialization;

namespace SportZone.Application.Dtos;
public class FeatureDto
{
    public required int Id { get; set; }
    public required string Name { get; set; }
    public string? Desc { get; set; }
    public bool? IsBanner { get; set; }
    public string? ImgUrl { get; set; }
   
    public List<ProductInFeatureDto>? Products { get; set; } 
}

public class UpdateFeatureDto
{
    public required int Id { get; set; }
    public required string Name { get; set; }
    public string? Desc { get; set; }
    public bool? IsBanner { get; set; }
    public string? ImgUrl { get; set; }
    public List<int>? ProductIds { get; set; } 
}

public class CreateFeatureDto
{
    public required string Name { get; set; }
    public string? Desc { get; set; }
    public string? ImgUrl { get; set; }
}