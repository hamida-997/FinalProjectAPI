using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FinalProjectAPI.Entities;
using FinalProjectAPI.Helpers.DTOs.Auth;
using FinalProjectAPI.Helpers.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.IdentityModel.Tokens;

namespace FinalProjectAPI.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly IConfiguration _configuration;
        private readonly IEmailService _emailService;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            UserManager<AppUser> userManager,
            SignInManager<AppUser> signInManager,
            IConfiguration configuration,
            IEmailService emailService,
            ILogger<AuthService> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto)
        {
            var existingEmail = await _userManager.FindByEmailAsync(registerDto.Email);
            if (existingEmail != null)
            {
                return new AuthResponseDto
                {
                    IsSuccess = false,
                    Message = "Email is already registered."
                };
            }

            var existingUsername = await _userManager.FindByNameAsync(registerDto.UserName);
            if (existingUsername != null)
            {
                return new AuthResponseDto
                {
                    IsSuccess = false,
                    Message = "Username is already taken."
                };
            }

            var user = new AppUser
            {
                UserName = registerDto.UserName,
                Email = registerDto.Email,
                PhoneNumber = registerDto.PhoneNumber,
                FirstName = registerDto.FirstName,
                LastName = registerDto.LastName,
                Address = registerDto.Address,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            var result = await _userManager.CreateAsync(user, registerDto.Password);

            if (!result.Succeeded)
            {
                var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                return new AuthResponseDto
                {
                    IsSuccess = false,
                    Message = $"Registration failed: {errors}"
                };
            }

            // Assign Customer role to new user
            var assignedRole = UserRole.Customer.ToString();
            await _userManager.AddToRoleAsync(user, assignedRole);

            // No login token yet: the user must confirm their email first
            var sent = await SendConfirmationEmailAsync(user);

            return new AuthResponseDto
            {
                IsSuccess = true,
                RequiresEmailConfirmation = true,
                Message = sent
                    ? "Registration successful. Please check your email to confirm your account."
                    : "Your account was created, but we couldn't send the confirmation email. Please use 'Resend confirmation email'."
            };
        }

        public async Task<AuthResponseDto> ConfirmEmailAsync(string userId, string token)
        {
            var invalid = new AuthResponseDto
            {
                IsSuccess = false,
                Message = "This confirmation link is invalid or has expired. Please request a new one."
            };

            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(token))
                return invalid;

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return invalid;

            if (user.EmailConfirmed)
                return new AuthResponseDto { IsSuccess = true, Message = "Your email is already confirmed. You can log in." };

            string decodedToken;
            try
            {
                decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));
            }
            catch (FormatException)
            {
                return invalid;
            }

            var result = await _userManager.ConfirmEmailAsync(user, decodedToken);
            if (!result.Succeeded)
                return invalid;

            return new AuthResponseDto { IsSuccess = true, Message = "Your email is confirmed. You can now log in." };
        }

        public async Task ResendConfirmationAsync(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null || user.EmailConfirmed || !user.IsActive)
                return;

            await SendConfirmationEmailAsync(user);
        }

        private async Task<bool> SendConfirmationEmailAsync(AppUser user)
        {
            var mvcBaseUrl = (_configuration["AppSettings:MvcBaseUrl"] ?? "https://localhost:7069").TrimEnd('/');

            // Identity tokens contain + / = which break in URLs, so Base64Url-encode them
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
            var link = $"{mvcBaseUrl}/Auth/ConfirmEmail?userId={Uri.EscapeDataString(user.Id)}&token={encodedToken}";

            var name = System.Net.WebUtility.HtmlEncode(user.FirstName);
            var body = $@"
                <div style=""font-family: Arial, sans-serif; max-width: 520px; margin: auto; color: #333;"">
                    <h2 style=""color: #8B4513;"">Welcome to Caramellize, {name}!</h2>
                    <p>Please confirm your email address to activate your account.</p>
                    <p style=""margin: 30px 0;"">
                        <a href=""{link}"" style=""background: #8B4513; color: #fff; padding: 12px 24px; border-radius: 8px; text-decoration: none; font-weight: bold;"">
                            Confirm my email
                        </a>
                    </p>
                    <p style=""font-size: 13px; color: #777;"">If the button doesn't work, copy this link into your browser:<br>{link}</p>
                    <p style=""font-size: 13px; color: #777;"">The link expires in 24 hours. If you didn't create an account, you can ignore this email.</p>
                </div>";

            var sent = await _emailService.SendEmailAsync(user.Email!, $"{user.FirstName} {user.LastName}".Trim(),
                "Confirm your Caramellize account", body);

            if (!sent)
                _logger.LogError("Confirmation email to user {UserId} could not be sent", user.Id);

            return sent;
        }

        public async Task<AuthResponseDto> LoginAsync(LoginDto loginDto)
        {
            var user = await _userManager.FindByEmailAsync(loginDto.Email);
            if (user == null)
            {
                return new AuthResponseDto
                {
                    IsSuccess = false,
                    Message = "Invalid email or password."
                };
            }

            if (!user.IsActive)
            {
                return new AuthResponseDto
                {
                    IsSuccess = false,
                    Message = "Your account is deactivated. Please contact support."
                };
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, loginDto.Password, lockoutOnFailure: true);

            if (result.IsLockedOut)
            {
                return new AuthResponseDto
                {
                    IsSuccess = false,
                    Message = "Account locked due to too many failed attempts. Try again later."
                };
            }

            if (!result.Succeeded)
            {
                return new AuthResponseDto
                {
                    IsSuccess = false,
                    Message = "Invalid email or password."
                };
            }

            // Checked after the password so this message never reveals an account to a stranger
            if (!user.EmailConfirmed)
            {
                return new AuthResponseDto
                {
                    IsSuccess = false,
                    EmailNotConfirmed = true,
                    Message = "Please confirm your email before logging in. Check your inbox (and spam folder)."
                };
            }

            var roles = await _userManager.GetRolesAsync(user);

            var token = GenerateJwtToken(user, roles.ToList());
            var expiresAt = DateTime.UtcNow.AddMinutes(
                Convert.ToDouble(_configuration["JwtSettings:ExpirationInMinutes"] ?? "60"));

            return new AuthResponseDto
            {
                IsSuccess = true,
                Message = "Login successful.",
                Token = token,
                ExpiresAt = expiresAt,
                User = new UserInfoDto
                {
                    Id = user.Id,
                    UserName = user.UserName ?? string.Empty,
                    Email = user.Email ?? string.Empty,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    PhoneNumber = user.PhoneNumber,
                    Address = user.Address,
                    Roles = roles.ToList()
                }
            };
        }

        private string GenerateJwtToken(AppUser user, List<string> roles)
        {
            var secretKey = _configuration["JwtSettings:SecretKey"]
                ?? throw new InvalidOperationException("JWT SecretKey is not configured.");
            var issuer = _configuration["JwtSettings:Issuer"];
            var audience = _configuration["JwtSettings:Audience"];
            var expirationInMinutes = Convert.ToDouble(
                _configuration["JwtSettings:ExpirationInMinutes"] ?? "60");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.UserName ?? string.Empty),
                new Claim("FirstName", user.FirstName),
                new Claim("LastName", user.LastName)
            };

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expirationInMinutes),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
