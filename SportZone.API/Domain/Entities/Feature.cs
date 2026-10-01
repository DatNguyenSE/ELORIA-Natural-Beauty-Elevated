
namespace SportZone.Domain.Entities;
public class Feature
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? Desc { get; set; } 
    public string? ImgUrl { get; set; }
    public bool? IsBanner { get; set; }
    public ICollection<Product> Products { get; set; } = new List<Product>();
}