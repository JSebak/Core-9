using Business.Services;
using Domain.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;
        private readonly ILogger<AuthController> _logger;
        private readonly IStringLocalizer<AuthController> _localizer;

        public AuthController(AuthService authService, ILogger<AuthController> logger, IStringLocalizer<AuthController> localizer)
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
                return Ok(_localizer["LoginSuccessfull"].Value ?? "Login Successfull.");
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized login attempt for user {Email}.", loginModel.Email);
                return Unauthorized(_localizer["InvalidEmailPassword"].Value ?? "Invalid email or password.");
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
    }
}
