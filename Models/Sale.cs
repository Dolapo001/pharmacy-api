using System.ComponentModel.DataAnnotations;

namespace PharmacyAPI.Models
{
    public class Sale
    {
        public int Id { get; set; }
        
        [Required]
        public int CustomerId { get; set; }
        public Customer Customer { get; set; } = null!;
        
        [Required]
        public int UserId { get; set; }
        public User User { get; set; } = null!;
        
        [Required]
        public decimal TotalAmount { get; set; }
        
        public DateTime SaleDate { get; set; } = DateTime.UtcNow;
        
        public List<SaleItem> SaleItems { get; set; } = new();
    }
    
    public class SaleItem
    {
        public int Id { get; set; }
        
        [Required]
        public int SaleId { get; set; }
        public Sale Sale { get; set; } = null!;
        
        [Required]
        public int MedicineId { get; set; }
        public Medicine Medicine { get; set; } = null!;
        
        [Required]
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }
        
        [Required]
        public decimal UnitPrice { get; set; }
        
        [Required]
        public decimal TotalPrice { get; set; }
    }
}