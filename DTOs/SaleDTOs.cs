using System.ComponentModel.DataAnnotations;

namespace PharmacyAPI.DTOs
{
    public class SaleCreateDTO
    {
        [Required] public int CustomerId { get; set; }
        [Required] public int UserId { get; set; }
        [Required] public List<SaleItemDTO> Items { get; set; } = new();
    }

    public class SaleItemDTO
    {
        [Required] public int MedicineId { get; set; }
        [Required] [Range(1, int.MaxValue)] public int Quantity { get; set; }
    }

    public class SaleResponseDTO
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public int UserId { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime SaleDate { get; set; }
        public List<SaleItemResponseDTO> Items { get; set; } = new();
    }

    public class SaleItemResponseDTO
    {
        public int MedicineId { get; set; }
        public string MedicineName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
    }
}