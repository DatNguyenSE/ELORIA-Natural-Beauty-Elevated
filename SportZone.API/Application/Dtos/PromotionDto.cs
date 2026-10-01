using System.ComponentModel.DataAnnotations;

namespace SportZone.Application.Dtos
{
    public class PromotionDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = "";
        public string DiscountType { get; set; } = "PERCENT";
        public decimal DiscountValue { get; set; }

        public decimal DiscountAmount { get; set; }
        public decimal? MinOrderValue { get; set; }  // apply for bills over this value

         public decimal? MaxDiscountAmount { get; set; } // cap for percent discounts
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; }
        
        public string? Title { get; set; }      
        public string? Description { get; set; } 
        public string? Icon { get; set; }       
        public bool IsVisible { get; set; } = true; 
    
    }
}
