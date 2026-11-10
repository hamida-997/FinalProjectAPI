using FinalProjectAPI.Helpers.DTOs.Auth;
using FinalProjectAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace FinalProjectAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("Register")]
        public async Task<ActionResult<AuthResponseDto>> Register(RegisterDto registerDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _authService.RegisterAsync(registerDto);

            if (!result.IsSuccess)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        [HttpPost("Login")]
        public async Task<ActionResult<AuthResponseDto>> Login(LoginDto loginDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _authService.LoginAsync(loginDto);

            if (result.EmailNotConfirmed)
            {
                // 403: credentials are right, but the account isn't confirmed yet
                return StatusCode(StatusCodes.Status403Forbidden, result);
            }

            if (!result.IsSuccess)
            {
                return Unauthorized(result);
            }

            return Ok(result);
        }

        // GET: api/Auth/confirm-email?userId=...&token=...
        [HttpGet("confirm-email")]
        public async Task<ActionResult<AuthResponseDto>> ConfirmEmail([FromQuery] string userId, [FromQuery] string token)
        {
            var result = await _authService.ConfirmEmailAsync(userId, token);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        // POST: api/Auth/resend-confirmation
        // Always returns the same answer so it can't be used to check which emails are registered
        [HttpPost("resend-confirmation")]
        public async Task<IActionResult> ResendConfirmation(ResendConfirmationDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            await _authService.ResendConfirmationAsync(dto.Email);
            return Ok(new { message = "If that email belongs to an unconfirmed account, a new confirmation link has been sent." });
        }
    }
}
