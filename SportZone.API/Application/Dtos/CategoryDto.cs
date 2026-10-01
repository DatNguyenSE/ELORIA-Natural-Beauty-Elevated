using System;

namespace SportZone.Application.Dtos;

public class CategoryDto
{
    public int Id { get; set; }
    public required string CategoryName { get; set; } 
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }

}