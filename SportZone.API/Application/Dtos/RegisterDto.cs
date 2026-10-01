using System.ComponentModel.DataAnnotations;

namespace SportZone.Application.Dtos
{
    public class RegisterDto
    {
        public string? UserName { get;set; }
        [Required]
        [EmailAddress]
        public string Email { get; set; } = "";

        [Required]
        [MinLength(4)]
        public string Password { get; set; } = "";  
    }
}