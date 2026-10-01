namespace SportZone.Application.Dtos
{
    public class MemberDto
    {
        public string? Id { get; set; }
        public string? Email { get; set; }
        public string? UserName { get; set; }
        public string? FullName { get; set; }
        public string? ImageUrl { get; set; }
        public string? Address { get; set; }
        public string? Gender { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public int? Points { get; set; }  
        public List<OrderDto>? Orders { get; set; } 
        // public List<Review> Reviews { get; set; }

    }

    public class MemberIdRequest {
    public required string MemberId { get; set; }
}
    
} 