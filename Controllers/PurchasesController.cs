// using Microsoft.AspNetCore.Authorization;
// using Microsoft.AspNetCore.Mvc;
// using Microsoft.EntityFrameworkCore;
// using PharmacyAPI.Data;
// using PharmacyAPI.DTOs;
// using PharmacyAPI.Models;
//
// namespace PharmacyAPI.Controllers
// {
//     [ApiController]
//     [Route("api/[controller]")]
//     [Authorize]
//     public class PurchasesController : ControllerBase
//     {
//         private readonly PharmacyContext _context;
//
//         public PurchasesController(PharmacyContext context)
//         {
//             _context = context;
//         }
//
//         [HttpPost]
//         public async Task<ActionResult<PurchaseResponseDTO>> CreatePurchase(PurchaseCreateDTO request)
//         {
//             var medicine = await _context.Medicines.FindAsync(request.MedicineId);
//             if (medicine == null || !medicine.IsActive) 
//                 return BadRequest("Medicine not found");
//
//             // Update inventory
//             medicine.Quantity += request.Quantity;
//             
//             var purchase = new Purchase
//             {
//                 MedicineId = request.MedicineId,
//                 Quantity = request.Quantity,
//                 UnitCost = request.UnitCost,
//                 TotalCost = request.Quantity * request.UnitCost,
//                 Supplier = request.Supplier,
//                 UserId = request.UserId,
//                 PurchaseDate = DateTime.UtcNow
//             };
//
//             _context.Purchases.Add(purchase);
//             await _context.SaveChangesAsync();
//
//             var response = new PurchaseResponseDTO
//             {
//                 Id = purchase.Id,
//                 MedicineId = purchase.MedicineId,
//                 MedicineName = medicine.Name,
//                 Quantity = purchase.Quantity,
//                 UnitCost = purchase.UnitCost,
//                 TotalCost = purchase.TotalCost,
//                 Supplier = purchase.Supplier,
//                 PurchaseDate = purchase.PurchaseDate,
//                 UserId = purchase.UserId
//             };
//
//             return CreatedAtAction(nameof(GetPurchase), new { id = purchase.Id }, response);
//         }
//
//         [HttpGet("{id}")]
//         public async Task<ActionResult<PurchaseResponseDTO>> GetPurchase(int id)
//         {
//             var purchase = await _context.Purchases
//                 .Include(p => p.Medicine)
//                 .FirstOrDefaultAsync(p => p.Id == id);
//
//             if (purchase == null) return NotFound();
//
//             return new PurchaseResponseDTO
//             {
//                 Id = purchase.Id,
//                 MedicineId = purchase.MedicineId,
//                 MedicineName = purchase.Medicine.Name,
//                 Quantity = purchase.Quantity,
//                 UnitCost = purchase.UnitCost,
//                 TotalCost = purchase.TotalCost,
//                 Supplier = purchase.Supplier,
//                 PurchaseDate = purchase.PurchaseDate,
//                 UserId = purchase.UserId
//             };
//         }
//
//         [HttpGet]
//         public async Task<ActionResult<IEnumerable<PurchaseResponseDTO>>> GetPurchases()
//         {
//             return await _context.Purchases
//                 .Include(p => p.Medicine)
//                 .Select(p => new PurchaseResponseDTO
//                 {
//                     Id = p.Id,
//                     MedicineId = p.MedicineId,
//                     MedicineName = p.Medicine.Name,
//                     Quantity = p.Quantity,
//                     UnitCost = p.UnitCost,
//                     TotalCost = p.TotalCost,
//                     Supplier = p.Supplier,
//                     PurchaseDate = p.PurchaseDate,
//                     UserId = p.UserId
//                 })
//                 .ToListAsync();
//         }
//     }
// }