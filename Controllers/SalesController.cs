// using Microsoft.AspNetCore.Authorization;
// using Microsoft.AspNetCore.Mvc;
// using Microsoft.EntityFrameworkCore;
// using Npgsql;
// using PharmacyAPI.Data;
// using PharmacyAPI.DTOs;
// using PharmacyAPI.Models;
// using System.Data;
//
// namespace PharmacyAPI.Controllers
// {
//     [ApiController]
//     [Route("api/[controller]")]
//     [Authorize]
//     public class SalesController : ControllerBase
//     {
//         private readonly PharmacyContext _context;
//         private readonly ILogger<SalesController> _logger;
//
//         public SalesController(PharmacyContext context, ILogger<SalesController> logger)
//         {
//             _context = context;
//             _logger = logger;
//         }
//
//         [HttpPost]
//         public async Task<ActionResult<SaleResponseDTO>> CreateSale(SaleCreateDTO request)
//         {
//             // Get execution strategy
//             var strategy = _context.Database.CreateExecutionStrategy();
//             
//             return await strategy.ExecuteAsync<ActionResult<SaleResponseDTO>>(async () =>
//             {
//                 using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
//                 
//                 try
//                 {
//                     // Lock all medicine rows
//                     var medicineIds = request.Items.Select(i => i.MedicineId).Distinct().ToList();
//                     if (medicineIds.Count > 0)
//                     {
//                         var parameters = new List<NpgsqlParameter>();
//                         var paramNames = new List<string>();
//                         for (var i = 0; i < medicineIds.Count; i++)
//                         {
//                             var paramName = $"@p{i}";
//                             paramNames.Add(paramName);
//                             parameters.Add(new NpgsqlParameter(paramName, medicineIds[i]));
//                         }
//
//                         var query = $"SELECT * FROM \"Medicines\" WHERE \"Id\" IN ({string.Join(",", paramNames)}) FOR UPDATE";
//                         await _context.Database.ExecuteSqlRawAsync(query, parameters);
//                     }
//
//                     // Create sale record
//                     var sale = new Sale
//                     {
//                         CustomerId = request.CustomerId,
//                         UserId = request.UserId,
//                         SaleDate = DateTime.UtcNow
//                     };
//                     _context.Sales.Add(sale);
//                     await _context.SaveChangesAsync();
//
//                     // Process sale items
//                     decimal totalAmount = 0;
//                     var items = new List<SaleItem>();
//                     
//                     foreach (var item in request.Items)
//                     {
//                         var medicine = await _context.Medicines.FindAsync(item.MedicineId);
//                         if (medicine == null || !medicine.IsActive)
//                             return BadRequest($"Medicine {item.MedicineId} not found");
//                         
//                         if (medicine.Quantity < item.Quantity)
//                             return BadRequest($"Insufficient stock for {medicine.Name}");
//
//                         // Update inventory
//                         medicine.Quantity -= item.Quantity;
//                         
//                         // Create sale item
//                         var saleItem = new SaleItem
//                         {
//                             SaleId = sale.Id,
//                             MedicineId = item.MedicineId,
//                             Quantity = item.Quantity,
//                             UnitPrice = medicine.Price,
//                             TotalPrice = medicine.Price * item.Quantity
//                         };
//                         
//                         totalAmount += saleItem.TotalPrice;
//                         items.Add(saleItem);
//                     }
//
//                     // Save sale items
//                     await _context.SaleItems.AddRangeAsync(items);
//                     
//                     // Update sale total
//                     sale.TotalAmount = totalAmount;
//                     await _context.SaveChangesAsync();
//                     await transaction.CommitAsync();
//
//                     // Prepare response
//                     var response = new SaleResponseDTO
//                     {
//                         Id = sale.Id,
//                         CustomerId = sale.CustomerId,
//                         UserId = sale.UserId,
//                         TotalAmount = sale.TotalAmount,
//                         SaleDate = sale.SaleDate,
//                         Items = items.Select(i => new SaleItemResponseDTO
//                         {
//                             MedicineId = i.MedicineId,
//                             MedicineName = _context.Medicines.Find(i.MedicineId)!.Name,
//                             Quantity = i.Quantity,
//                             UnitPrice = i.UnitPrice,
//                             TotalPrice = i.TotalPrice
//                         }).ToList()
//                     };
//
//                     return CreatedAtAction(nameof(GetSale), new { id = sale.Id }, response);
//                 }
//                 catch (Exception ex)
//                 {
//                     await transaction.RollbackAsync();
//                     _logger.LogError(ex, "Error processing sale");
//                     return StatusCode(500, $"Error processing sale: {ex.Message}");
//                 }
//             });
//         }
//
//         [HttpGet("{id}")]
//         public async Task<ActionResult<SaleResponseDTO>> GetSale(int id)
//         {
//             var sale = await _context.Sales
//                 .Include(s => s.SaleItems)
//                 .FirstOrDefaultAsync(s => s.Id == id);
//
//             if (sale == null) return NotFound();
//
//             return new SaleResponseDTO
//             {
//                 Id = sale.Id,
//                 CustomerId = sale.CustomerId,
//                 UserId = sale.UserId,
//                 TotalAmount = sale.TotalAmount,
//                 SaleDate = sale.SaleDate,
//                 Items = sale.SaleItems.Select(i => new SaleItemResponseDTO
//                 {
//                     MedicineId = i.MedicineId,
//                     MedicineName = _context.Medicines.Find(i.MedicineId)!.Name,
//                     Quantity = i.Quantity,
//                     UnitPrice = i.UnitPrice,
//                     TotalPrice = i.TotalPrice
//                 }).ToList()
//             };
//         }
//
//         [HttpGet("customer/{customerId}")]
//         public async Task<ActionResult<IEnumerable<SaleResponseDTO>>> GetCustomerSales(int customerId)
//         {
//             return await _context.Sales
//                 .Where(s => s.CustomerId == customerId)
//                 .Select(s => new SaleResponseDTO
//                 {
//                     Id = s.Id,
//                     CustomerId = s.CustomerId,
//                     UserId = s.UserId,
//                     TotalAmount = s.TotalAmount,
//                     SaleDate = s.SaleDate
//                 })
//                 .ToListAsync();
//         }
//     }
// }
