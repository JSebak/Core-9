using Business.Interfaces;
using Domain.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;
        private readonly IStringLocalizer<AuthController> _localizer;

        public AuthController(IAuthService authService, ILogger<AuthController> logger, IStringLocalizer<AuthController> localizer)
        {
            _authService = authService;
            _localizer = localizer;
            _logger = logger;
        }

        [HttpPost("Login")]
        public async Task<IActionResult> Login([FromBody] UserLoginModel loginModel)
        {
            if (loginModel == null)
            {
                _logger.LogWarning("Login attempt with empty data.");
                return BadRequest(_localizer["InvalidLogin"].Value ?? "Invalid login request.");
            }

            try
            {
                var token = await _authService.Login(loginModel);

                if (token == null)
                {
                    _logger.LogWarning("Failed login attempt for user {Email}.", loginModel.Email);
                    return Unauthorized(_localizer["InvalidEmailPassword"].Value ?? "Invalid email or password.");
                }

                var cookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTime.UtcNow.AddHours(1)
                };
                Response.Cookies.Append("AuthToken", token, cookieOptions);
                return Ok(_localizer["LoginSuccessfull"].Value ?? "Login Successful.");
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized login attempt for user {Email}.", loginModel.Email);
                return Unauthorized(_localizer["InvalidEmailPassword"].Value ?? "Invalid email or password.");
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid login attempt: {Message}.", ex.Message);
                return BadRequest(_localizer["InvalidLogin"].Value ?? ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred during login for user {Email}.", loginModel.Email);
                return StatusCode(500, _localizer["UnexpectedError"].Value ?? "An error occurred while processing your request.");
            }
        }

        [HttpPost("Logout")]
        public async Task<IActionResult> Logout()
        {
            try
            {
                Response.Cookies.Delete("AuthToken");
                return Ok(_localizer["LogoutSuccessfull"].Value ?? "Logout successful.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred during logout.");
                return StatusCode(500, _localizer["UnexpectedError"].Value ?? "An error occurred while processing your request.");
            }
        }

        [HttpPost("Verify")]
        public async Task<IActionResult> Verify([FromQuery] string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                _logger.LogWarning("Verification attempt with missing or invalid token.");
                return BadRequest(_localizer["InvalidToken"].Value ?? "Invalid token.");
            }

            try
            {
                await _authService.VerifyAccount(token);
                return Ok(_localizer["AccountVerified"].Value ?? "Account verified successfully.");
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized account verification attempt with token {Token}.", token);
                return Unauthorized(_localizer["InvalidToken"].Value ?? "Invalid or expired token.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred during account verification with token {Token}.", token);
                return StatusCode(500, _localizer["UnexpectedError"].Value ?? "An error occurred while processing your request.");
            }
        }

    }
}
