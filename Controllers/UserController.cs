using EnergyManagementSystem.Data;
using EnergyManagementSystem.Models;
using EnergyManagementSystem.Models.DTOs;
using EnergyManagementSystem.Services;
using Microsoft.AspNetCore.Mvc;

namespace EnergyManagementSystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly JwtService _jwtService;
        private readonly AppDbContext _context;

        public object BCrypt { get; private set; }

        public UserController(JwtService jwtService, AppDbContext context)
        {
            _jwtService = jwtService;
            _context = context;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto dto)
        {
            var userExists = await _context.Users.AnyAsync(u => u.Username == dto.Username);
            if (userExists) return BadRequest("User already exists");

            var passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
            var user = new User { Username = dto.Username, PasswordHash = passwordHash };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok("User registered successfully");
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == dto.Username);
            if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                return Unauthorized("Invalid credentials");

            var token = _jwtService.GenerateToken(user.Username);
            return Ok(new { Token = token });
        }
    }


}
