using System.ComponentModel.DataAnnotations;

namespace PharmacyAPI.DTOs
{
    public class PurchaseCreateDTO
    {
        [Required] public int MedicineId { get; set; }
        [Required] public int UserId { get; set; }
        [Required] [Range(1, int.MaxValue)] public int Quantity { get; set; }
        [Required] public decimal UnitCost { get; set; }
        [Required] public string Supplier { get; set; } = string.Empty;
    }

    public class PurchaseResponseDTO
    {
        public int Id { get; set; }
        public int MedicineId { get; set; }
        public string MedicineName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitCost { get; set; }
        public decimal TotalCost { get; set; }
        public string Supplier { get; set; } = string.Empty;
        public DateTime PurchaseDate { get; set; }
        public int UserId { get; set; }
    }
}