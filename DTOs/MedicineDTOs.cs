using System.ComponentModel.DataAnnotations;

namespace PharmacyAPI.DTOs
{
    public class CreateMedicineDTO
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;
        
        public string Description { get; set; } = string.Empty;
        
        [Required]
        public string Category { get; set; } = string.Empty;
        
        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal Price { get; set; }
        
        [Required]
        [Range(0, int.MaxValue)]
        public int Quantity { get; set; }
        
        [Required]
        public DateTime ExpiryDate { get; set; }
    }
    
    public class UpdateMedicineDTO : CreateMedicineDTO
    {
        public int Id { get; set; }
    }
    
    public class MedicineResponseDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public DateTime ExpiryDate { get; set; }
        public bool IsExpired => ExpiryDate < DateTime.UtcNow;
        public bool IsLowStock => Quantity < 10;
    }
}