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
//     [Authorize(Roles = "Admin")]
//     public class AdminController : ControllerBase
//     {
//         private readonly PharmacyContext _context;
//
//         public AdminController(PharmacyContext context)
//         {
//             _context = context;
//         }
//
//         [HttpGet("users")]
//         public async Task<ActionResult<IEnumerable<UserResponseDTO>>> GetUsers()
//         {
//             return await _context.Users
//                 .Select(u => new UserResponseDTO
//                 {
//                     Id = u.Id,
//                     Username = u.Username,
//                     Email = u.Email,
//                     Role = u.Role,
//                     CreatedAt = u.CreatedAt,
//                     IsActive = u.IsActive
//                 })
//                 .ToListAsync();
//         }
//
//         [HttpPost("users/{id}/activate")]
//         public async Task<IActionResult> ActivateUser(int id)
//         {
//             var user = await _context.Users.FindAsync(id);
//             if (user == null) return NotFound();
//
//             user.IsActive = true;
//             await _context.SaveChangesAsync();
//             return NoContent();
//         }
//
//         [HttpPost("users/{id}/deactivate")]
//         public async Task<IActionResult> DeactivateUser(int id)
//         {
//             var user = await _context.Users.FindAsync(id);
//             if (user == null) return NotFound();
//
//             user.IsActive = false;
//             await _context.SaveChangesAsync();
//             return NoContent();
//         }
//
//         [HttpGet("reports/sales")]
//         public async Task<ActionResult<SalesReportDTO>> GetSalesReport(
//             [FromQuery] DateTime startDate, 
//             [FromQuery] DateTime endDate)
//         {
//             var sales = await _context.Sales
//                 .Where(s => s.SaleDate >= startDate && s.SaleDate <= endDate)
//                 .GroupBy(s => s.SaleDate.Date)
//                 .Select(g => new DailySalesDTO
//                 {
//                     Date = g.Key,
//                     TotalSales = g.Sum(s => s.TotalAmount),
//                     Count = g.Count()
//                 })
//                 .ToListAsync();
//
//             return new SalesReportDTO
//             {
//                 StartDate = startDate,
//                 EndDate = endDate,
//                 TotalRevenue = sales.Sum(d => d.TotalSales),
//                 DailySales = sales
//             };
//         }
//     }
// }