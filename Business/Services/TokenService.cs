using Business.Interfaces;
using Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Business.Services
{
    public class TokenService : ITokenService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<TokenService> _logger;

        public TokenService(IConfiguration configuration, ILogger<TokenService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public string GenerateToken(User user)
        {
            try
            {
                var jwtSettings = _configuration.GetSection("Jwt");
                if (jwtSettings == null || string.IsNullOrEmpty(jwtSettings["Key"]) || string.IsNullOrEmpty(jwtSettings["Issuer"]) || string.IsNullOrEmpty(jwtSettings["Audience"]))
                {
                    _logger.LogError("JWT configuration is missing or incomplete.");
                    throw new InvalidOperationException("Invalid JWT configuration.");
                }

                var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]));
                var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

                var claims = new[]
                {
                    new Claim(JwtRegisteredClaimNames.Sub, user.Username),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Role, user.Role.ToString())
                };

                if (!double.TryParse(jwtSettings["ExpirationMinutes"], out var expirationMinutes))
                {
                    _logger.LogError("Invalid JWT expiration time configured.");
                    throw new FormatException("Invalid JWT expiration time configuration.");
                }

                var token = new JwtSecurityToken(
                    issuer: jwtSettings["Issuer"],
                    audience: jwtSettings["Audience"],
                    claims: claims,
                    expires: DateTime.Now.AddMinutes(expirationMinutes),
                    signingCredentials: credentials
                );

                return new JwtSecurityTokenHandler().WriteToken(token);
            }
            catch (SecurityTokenException ex)
            {
                _logger.LogError(ex, "An error occurred while creating the JWT token for user {Username}.", user.Username);
                throw new Exception("An error occurred while generating the token. Please try again later.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while generating the token for user {Username}.", user.Username);
                throw new Exception("An unexpected error occurred. Please try again later.", ex);
            }
        }
    }
}
