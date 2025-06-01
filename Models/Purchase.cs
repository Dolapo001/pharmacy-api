using System.ComponentModel.DataAnnotations;

namespace PharmacyAPI.Models
{
    public class Purchase
    {
        public int Id { get; set; }
        
        [Required]
        public int MedicineId { get; set; }
        public Medicine Medicine { get; set; } = null!;
        
        [Required]
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }
        
        [Required]
        public decimal UnitCost { get; set; }
        
        [Required]
        public decimal TotalCost { get; set; }
        
        [StringLength(100)]
        public string Supplier { get; set; } = string.Empty;
        
        public DateTime PurchaseDate { get; set; } = DateTime.UtcNow;
        
        [Required]
        public int UserId { get; set; }
        public User User { get; set; } = null!;
    }
}