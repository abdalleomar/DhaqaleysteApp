using DhaqaleysteApi.Data;
using DhaqaleysteApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DhaqaleysteApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UsersController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(User user)
        {
            var exists = await _context.Users
                .AnyAsync(x => x.Email == user.Email || x.Phone == user.Phone);

            if (exists)
                return BadRequest("User already exists");

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(user);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(User login)
        {
            var user = await _context.Users.FirstOrDefaultAsync(x =>
                (x.Email == login.Email || x.Phone == login.Phone) &&
                x.Password == login.Password);

            if (user == null)
                return Unauthorized("Invalid login");

            return Ok(user);
        }
    }
}