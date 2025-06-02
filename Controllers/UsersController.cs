using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmacyAPI.Data;
using PharmacyAPI.Models;

namespace PharmacyAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")] // Restrict to admins only
    public class UsersController : ControllerBase
    {
        private readonly PharmacyContext _context;

        public UsersController(PharmacyContext context)
        {
            _context = context;
        }

        // New endpoint to get total user count
        [HttpGet("count")]
        public async Task<ActionResult<int>> GetUserCount()
        {
            return await _context.Users.CountAsync();
        }
    }
}