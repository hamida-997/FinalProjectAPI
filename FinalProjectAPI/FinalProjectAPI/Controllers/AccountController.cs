using System.Security.Claims;
using AutoMapper;
using FinalProjectAPI.Data;
using FinalProjectAPI.Entities;
using FinalProjectAPI.Helpers.DTOs.Auth;
using FinalProjectAPI.Helpers.DTOs.Order;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinalProjectAPI.Controllers
{
    // Endpoints for the logged-in user's own account. The user is always taken
    // from the token, never from the request, so nobody can edit someone else.
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AccountController : ControllerBase
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;

        public AccountController(UserManager<AppUser> userManager, AppDbContext context, IMapper mapper)
        {
            _userManager = userManager;
            _context = context;
            _mapper = mapper;
        }

        // GET: api/Account/me
        [HttpGet("me")]
        public async Task<ActionResult<ProfileDto>> GetMe()
        {
            var user = await CurrentUserAsync();
            if (user == null) return Unauthorized();

            return Ok(await ToDtoAsync(user));
        }

        // PUT: api/Account/me
        [HttpPut("me")]
        public async Task<ActionResult<ProfileDto>> UpdateMe(UpdateProfileDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var user = await CurrentUserAsync();
            if (user == null) return Unauthorized();

            user.FirstName = dto.FirstName.Trim();
            user.LastName = dto.LastName.Trim();
            user.PhoneNumber = string.IsNullOrWhiteSpace(dto.PhoneNumber) ? null : dto.PhoneNumber.Trim();
            user.Address = string.IsNullOrWhiteSpace(dto.Address) ? null : dto.Address.Trim();
            user.UpdatedAt = DateTime.UtcNow;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                return BadRequest(new { message = string.Join(" ", result.Errors.Select(e => e.Description)) });
            }

            return Ok(await ToDtoAsync(user));
        }

        // POST: api/Account/change-password
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword(ChangeOwnPasswordDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var user = await CurrentUserAsync();
            if (user == null) return Unauthorized();

            var result = await _userManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);
            if (!result.Succeeded)
            {
                // e.g. "Incorrect password." or password rule messages
                return BadRequest(new { message = string.Join(" ", result.Errors.Select(e => e.Description)) });
            }

            return Ok(new { message = "Your password has been changed." });
        }

        // GET: api/Account/orders
        // Orders linked to this account, plus older orders placed with the same email
        [HttpGet("orders")]
        public async Task<ActionResult<IEnumerable<OrderDto>>> GetMyOrders()
        {
            var user = await CurrentUserAsync();
            if (user == null) return Unauthorized();

            var email = user.Email ?? string.Empty;
            var orders = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .Where(o => o.UserId == user.Id || (o.UserId == null && o.CustomerEmail == email))
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return Ok(_mapper.Map<IEnumerable<OrderDto>>(orders));
        }

        private async Task<AppUser?> CurrentUserAsync()
        {
            var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return string.IsNullOrEmpty(id) ? null : await _userManager.FindByIdAsync(id);
        }

        private async Task<ProfileDto> ToDtoAsync(AppUser user) => new()
        {
            Id = user.Id,
            UserName = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            FirstName = user.FirstName,
            LastName = user.LastName,
            PhoneNumber = user.PhoneNumber,
            Address = user.Address,
            CreatedAt = user.CreatedAt,
            Roles = (await _userManager.GetRolesAsync(user)).ToList()
        };
    }
}
