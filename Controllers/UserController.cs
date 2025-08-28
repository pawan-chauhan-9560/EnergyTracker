using BCrypt.Net;
using EnergyManagementSystem.Data;
using EnergyManagementSystem.Models;
using EnergyManagementSystem.Models.DTOs;
using EnergyManagementSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace EnergyManagementSystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly JwtService _jwtService;
        private readonly AppDbContext _context;

        public UserController(JwtService jwtService, AppDbContext context)
        {
            _jwtService = jwtService;
            _context = context;
        }
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                // 1. Check if user exists
                if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
                    return BadRequest(new { message = "Email already exists" });

                // 2. Determine if first user
                bool isFirstUser = !await _context.Users.AnyAsync();
                string roleName = isFirstUser ? "Admin" : "User";

                // 3. Create user
                var user = new User
                {
                    Username = dto.Username,
                    Email = dto.Email,
                    FirstName = dto.Firstname,
                    LastName = dto.Lastname,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password)
                };
                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // 4. Assign role
                var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == roleName);
                if (role == null)
                    return StatusCode(500, new { message = "Role not found" });

                _context.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id });
                await _context.SaveChangesAsync();

                // 5. Return safe user info
                return Ok(new
                {
                    Id = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    Roles = new[] { role.Name },
                    Message = "User registered successfully"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred", error = ex.Message });
            }
        }
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == dto.Username);
            if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                return Unauthorized(new { message = "Invalid credentials" });

            var token = await _jwtService.GenerateJwtTokenAsync(user);

            var roles = await _context.UserRoles
                .Where(ur => ur.UserId == user.Id)
                .Include(ur => ur.Role)
                .Select(ur => ur.Role.Name)
                .ToListAsync();

            return Ok(new
            {
                Token = token,
                Username = user.Username,
                Roles = roles,
                Message = "Logged in successfully"
            });
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("assign-role")]
        public async Task<IActionResult> AssignRole([FromBody] AssignRoleDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _context.Users.FindAsync(dto.UserId);
            var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == dto.RoleName);

            if (user == null || role == null)
                return NotFound(new { message = "User or Role not found" });

            if (await _context.UserRoles.AnyAsync(ur => ur.UserId == dto.UserId && ur.RoleId == role.Id))
                return BadRequest(new { message = "User already has this role" });

            _context.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id });
            await _context.SaveChangesAsync();

            var roles = await _context.UserRoles
                .Where(ur => ur.UserId == user.Id)
                .Include(ur => ur.Role)
                .Select(ur => ur.Role.Name)
                .ToListAsync();

            return Ok(new { Message = "Role assigned successfully", Roles = roles });
        }
    }
}
