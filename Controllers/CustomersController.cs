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
    public class CustomersController : ControllerBase
    {
        private readonly PharmacyContext _context;

        public CustomersController(PharmacyContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<CustomerResponseDTO>>> GetCustomers()
        {
            return await _context.Customers
                .Where(c => c.IsActive)
                .Select(c => new CustomerResponseDTO
                {
                    Id = c.Id,
                    Name = c.Name,
                    Phone = c.Phone,
                    Email = c.Email,
                    Address = c.Address
                })
                .ToListAsync();
        }
        
        [HttpGet("count")]
        public async Task<ActionResult<int>> GetCustomerCount()
        {
            return await _context.Customers
                // .Where(c => c.IsActive) // Only count active customers
                .CountAsync();
        }
        
        [HttpGet("{id}")]
        public async Task<ActionResult<CustomerResponseDTO>> GetCustomer(int id)
        {
            var customer = await _context.Customers
                .Where(c => c.Id == id && c.IsActive)
                .Select(c => new CustomerResponseDTO
                {
                    Id = c.Id,
                    Name = c.Name,
                    Phone = c.Phone,
                    Email = c.Email,
                    Address = c.Address
                })
                .FirstOrDefaultAsync();

            if (customer == null) return NotFound();
            return customer;
        }

        [HttpPost]
        public async Task<ActionResult<CustomerResponseDTO>> CreateCustomer(CustomerCreateDTO request)
        {
            var customer = new Customer
            {
                Name = request.Name,
                Phone = request.Phone,
                Email = request.Email,
                Address = request.Address
            };

            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();

            var response = new CustomerResponseDTO
            {
                Id = customer.Id,
                Name = customer.Name,
                Phone = customer.Phone,
                Email = customer.Email,
                Address = customer.Address
            };

            return CreatedAtAction(nameof(GetCustomer), new { id = customer.Id }, response);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCustomer(int id, CustomerCreateDTO request)
        {
            var customer = await _context.Customers.FindAsync(id);
            if (customer == null || !customer.IsActive) return NotFound();

            customer.Name = request.Name;
            customer.Phone = request.Phone;
            customer.Email = request.Email;
            customer.Address = request.Address;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCustomer(int id)
        {
            var customer = await _context.Customers.FindAsync(id);
            if (customer == null) return NotFound();

            customer.IsActive = false;
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
    
    
}