using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmacyAPI.Data;
using PharmacyAPI.DTOs;
using PharmacyAPI.Models;

namespace PharmacyAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class MedicinesController : ControllerBase
    {
        private readonly PharmacyContext _context;
        
        public MedicinesController(PharmacyContext context)
        {
            _context = context;
        }
        
        [HttpGet]
        public async Task<ActionResult<IEnumerable<MedicineResponseDTO>>> GetMedicines()
        {
            var medicines = await _context.Medicines
                .Where(m => m.IsActive)
                .Select(m => new MedicineResponseDTO
                {
                    Id = m.Id,
                    Name = m.Name,
                    Description = m.Description,
                    Category = m.Category,
                    Price = m.Price,
                    Quantity = m.Quantity,
                    ExpiryDate = m.ExpiryDate
                })
                .ToListAsync();
                
            return Ok(medicines);
        }
        
        [HttpGet("{id}")]
        public async Task<ActionResult<MedicineResponseDTO>> GetMedicine(int id)
        {
            var medicine = await _context.Medicines
                .Where(m => m.Id == id && m.IsActive)
                .Select(m => new MedicineResponseDTO
                {
                    Id = m.Id,
                    Name = m.Name,
                    Description = m.Description,
                    Category = m.Category,
                    Price = m.Price,
                    Quantity = m.Quantity,
                    ExpiryDate = m.ExpiryDate
                })
                .FirstOrDefaultAsync();
                
            if (medicine == null)
                return NotFound();
                
            return Ok(medicine);
        }
        
        [HttpPost]
        public async Task<ActionResult<MedicineResponseDTO>> CreateMedicine(CreateMedicineDTO request)
        {
            var medicine = new Medicine
            {
                Name = request.Name,
                Description = request.Description,
                Category = request.Category,
                Price = request.Price,
                Quantity = request.Quantity,
                ExpiryDate = request.ExpiryDate
            };
            
            _context.Medicines.Add(medicine);
            await _context.SaveChangesAsync();
            
            var response = new MedicineResponseDTO
            {
                Id = medicine.Id,
                Name = medicine.Name,
                Description = medicine.Description,
                Category = medicine.Category,
                Price = medicine.Price,
                Quantity = medicine.Quantity,
                ExpiryDate = medicine.ExpiryDate
            };
            
            return CreatedAtAction(nameof(GetMedicine), new { id = medicine.Id }, response);
        }
        
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateMedicine(int id, UpdateMedicineDTO request)
        {
            if (id != request.Id)
                return BadRequest();
                
            var medicine = await _context.Medicines.FindAsync(id);
            if (medicine == null || !medicine.IsActive)
                return NotFound();
                
            medicine.Name = request.Name;
            medicine.Description = request.Description;
            medicine.Category = request.Category;
            medicine.Price = request.Price;
            medicine.Quantity = request.Quantity;
            medicine.ExpiryDate = request.ExpiryDate;
            medicine.UpdatedAt = DateTime.UtcNow;
            
            await _context.SaveChangesAsync();
            return NoContent();
        }
        
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMedicine(int id)
        {
            var medicine = await _context.Medicines.FindAsync(id);
            if (medicine == null)
                return NotFound();
                
            medicine.IsActive = false;
            await _context.SaveChangesAsync();
            
            return NoContent();
        }
        
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<MedicineResponseDTO>>> SearchMedicines(
            [FromQuery] string? name, 
            [FromQuery] string? category)
        {
            var query = _context.Medicines.Where(m => m.IsActive);
    
            if (!string.IsNullOrEmpty(name))
                query = query.Where(m => EF.Functions.ILike(m.Name, $"%{name}%"));
        
            if (!string.IsNullOrEmpty(category))
                query = query.Where(m => EF.Functions.ILike(m.Category, $"%{category}%"));
    
            var medicines = await query
                .Select(m => new MedicineResponseDTO
                {
                    Id = m.Id,
                    Name = m.Name,
                    Description = m.Description,
                    Category = m.Category,
                    Price = m.Price,
                    Quantity = m.Quantity,
                    ExpiryDate = m.ExpiryDate
                })
                .ToListAsync();
        
            return Ok(medicines);
        }
    }
}