namespace SportZone.Domain.Entities
{
    public class ComboItem
    {
        public int Id { get; set; }

        public int ComboProductId { get; set; }
        public Product? ComboProduct { get; set; }

        public int ComponentProductId { get; set; }
        public Product? ComponentProduct { get; set; }

        public int Quantity { get; set; } = 1;
    }
}
