using AutoMapper;
using FinalProjectAPI.Entities;
using FinalProjectAPI.Helpers.DTOs.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinalProjectAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class UsersController : ControllerBase
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly IMapper _mapper;

        public UsersController(UserManager<AppUser> userManager, IMapper mapper)
        {
            _userManager = userManager;
            _mapper = mapper;
        }

        // GET: api/Users
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers([FromQuery] string? search = null, [FromQuery] string? role = null)
        {
            var usersQuery = _userManager.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.ToLower();
                usersQuery = usersQuery.Where(u => 
                    u.UserName.ToLower().Contains(search) || 
                    u.Email.ToLower().Contains(search) ||
                    u.FirstName.ToLower().Contains(search) ||
                    u.LastName.ToLower().Contains(search));
            }

            var users = await usersQuery
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();

            var userDtos = new List<UserDto>();

            foreach (var user in users)
            {
                var userDto = _mapper.Map<UserDto>(user);
                userDto.Roles = (await _userManager.GetRolesAsync(user)).ToList();

                // Filter by role if specified
                if (!string.IsNullOrWhiteSpace(role) && !userDto.Roles.Contains(role, StringComparer.OrdinalIgnoreCase))
                {
                    continue;
                }

                userDtos.Add(userDto);
            }

            return Ok(userDtos);
        }

        // GET: api/Users/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<UserDto>> GetUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            var userDto = _mapper.Map<UserDto>(user);
            userDto.Roles = (await _userManager.GetRolesAsync(user)).ToList();

            return Ok(userDto);
        }

        // POST: api/Users
        [HttpPost]
        public async Task<ActionResult<UserDto>> CreateUser(CreateUserDto createUserDto)
        {
            // Check if username exists
            var existingUser = await _userManager.FindByNameAsync(createUserDto.UserName);
            if (existingUser != null)
            {
                return BadRequest(new { message = "Username already exists" });
            }

            // Check if email exists
            existingUser = await _userManager.FindByEmailAsync(createUserDto.Email);
            if (existingUser != null)
            {
                return BadRequest(new { message = "Email already exists" });
            }

            // Validate roles
            if (createUserDto.Roles == null || createUserDto.Roles.Count == 0)
            {
                return BadRequest(new { message = "At least one role is required" });
            }

            var validRoles = new[] { "SuperAdmin", "Admin", "Customer" };
            foreach (var role in createUserDto.Roles)
            {
                if (!validRoles.Contains(role, StringComparer.OrdinalIgnoreCase))
                {
                    return BadRequest(new { message = $"Invalid role: {role}" });
                }
            }

            var user = _mapper.Map<AppUser>(createUserDto);

            // Created by an admin, so no confirmation email is needed to log in
            user.EmailConfirmed = true;

            var result = await _userManager.CreateAsync(user, createUserDto.Password);

            if (!result.Succeeded)
            {
                return IdentityError("Failed to create user.", result);
            }

            // Add roles
            await _userManager.AddToRolesAsync(user, createUserDto.Roles);

            var userDto = _mapper.Map<UserDto>(user);
            userDto.Roles = createUserDto.Roles;

            return CreatedAtAction(nameof(GetUser), new { id = user.Id }, userDto);
        }

        // PUT: api/Users/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(string id, UpdateUserDto updateUserDto)
        {
            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            // Validate roles
            if (updateUserDto.Roles == null || updateUserDto.Roles.Count == 0)
            {
                return BadRequest(new { message = "At least one role is required" });
            }

            var validRoles = new[] { "SuperAdmin", "Admin", "Customer" };
            foreach (var role in updateUserDto.Roles)
            {
                if (!validRoles.Contains(role, StringComparer.OrdinalIgnoreCase))
                {
                    return BadRequest(new { message = $"Invalid role: {role}" });
                }
            }

            // Don't let an admin lock themselves out of the admin panel
            var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (currentUserId == id)
            {
                var keepsAdmin = updateUserDto.Roles.Any(r =>
                    r.Equals("Admin", StringComparison.OrdinalIgnoreCase) ||
                    r.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase));
                if (!keepsAdmin)
                {
                    return BadRequest(new { message = "You cannot remove your own admin role." });
                }
                if (!updateUserDto.IsActive)
                {
                    return BadRequest(new { message = "You cannot disable your own account." });
                }
            }

            // Update basic properties
            _mapper.Map(updateUserDto, user);

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                return IdentityError("Failed to update user.", result);
            }

            // Update roles
            var currentRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
            await _userManager.AddToRolesAsync(user, updateUserDto.Roles);

            return NoContent();
        }

        // DELETE: api/Users/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            // Prevent deleting yourself
            var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (currentUserId == id)
            {
                return BadRequest(new { message = "You cannot delete yourself" });
            }

            var result = await _userManager.DeleteAsync(user);

            if (!result.Succeeded)
            {
                return IdentityError("Failed to delete user.", result);
            }

            return NoContent();
        }

        // POST: api/Users/{id}/change-password
        [HttpPost("{id}/change-password")]
        public async Task<IActionResult> ChangePassword(string id, ChangePasswordDto changePasswordDto)
        {
            if (id != changePasswordDto.UserId)
            {
                return BadRequest(new { message = "User ID mismatch" });
            }

            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            // Remove old password and add new one
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, changePasswordDto.NewPassword);

            if (!result.Succeeded)
            {
                return IdentityError("Failed to change password.", result);
            }

            return Ok(new { message = "Password changed successfully" });
        }

        // POST: api/Users/{id}/toggle-status
        [HttpPost("{id}/toggle-status")]
        public async Task<IActionResult> ToggleUserStatus(string id)
        {
            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            // Prevent disabling yourself
            var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (currentUserId == id)
            {
                return BadRequest(new { message = "You cannot disable yourself" });
            }

            user.IsActive = !user.IsActive;
            user.UpdatedAt = DateTime.UtcNow;

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                return IdentityError("Failed to update user status.", result);
            }

            return Ok(new { isActive = user.IsActive });
        }

        // Clients read "message" first, so put Identity's reasons (e.g. password rules) in it
        private BadRequestObjectResult IdentityError(string prefix, IdentityResult result)
        {
            var reasons = string.Join(" ", result.Errors.Select(e => e.Description));
            return BadRequest(new { message = string.IsNullOrEmpty(reasons) ? prefix : $"{prefix} {reasons}", errors = result.Errors });
        }

        // GET: api/Users/roles
        [HttpGet("roles")]
        public ActionResult<IEnumerable<string>> GetRoles()
        {
            return Ok(new[] { "SuperAdmin", "Admin", "Customer" });
        }

        // GET: api/Users/statistics
        [HttpGet("statistics")]
        public async Task<ActionResult> GetStatistics()
        {
            var totalUsers = await _userManager.Users.CountAsync();
            var activeUsers = await _userManager.Users.CountAsync(u => u.IsActive);
            var inactiveUsers = totalUsers - activeUsers;
            var newUsersThisMonth = await _userManager.Users
                .CountAsync(u => u.CreatedAt.Month == DateTime.UtcNow.Month && u.CreatedAt.Year == DateTime.UtcNow.Year);

            return Ok(new
            {
                totalUsers,
                activeUsers,
                inactiveUsers,
                newUsersThisMonth
            });
        }
    }
}
