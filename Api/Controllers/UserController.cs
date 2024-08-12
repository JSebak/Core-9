using Business.Interfaces;
using Domain.Interfaces;
using Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using System.Net;

namespace Api.Controllers
{
    [ApiController]
    [ApiExplorerSettings(GroupName = "User")]
    [Route("[controller]")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IAuthService _authService;
        private readonly ILogger<UserController> _logger;
        private readonly IStringLocalizer<UserController> _localizer;

        public UserController(IUserService userService, IAuthService authService, ILogger<UserController> logger, IStringLocalizer<UserController> localizer)
        {
            _userService = userService;
            _authService = authService;
            _logger = logger;
            _localizer = localizer;
        }

        private ActionResult HandleException(Exception ex, string action)
        {
            _logger.LogError(ex, $"An error occurred while {action}.");
            return StatusCode((int)HttpStatusCode.InternalServerError, _localizer["ErrorOccured"].Value);
        }

        private ActionResult HandleNotFoundException(KeyNotFoundException ex, string id, string entityName)
        {
            _logger.LogWarning(ex, $"{entityName} with ID {id} not found.");
            return NotFound(_localizer["NotFound"].Value ?? $"{entityName} with ID {id} not found.");
        }

        [Authorize(Roles = "Admin,Super")]
        [HttpGet(Name = "Get Users")]
        public async Task<ActionResult> GetAll()
        {
            try
            {
                var users = await _userService.GetAllUsers();
                return Ok(users);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "retrieving users");
            }
        }

        [Authorize]
        [HttpGet("{id}", Name = "Get By Id")]
        public async Task<ActionResult> Get(int id)
        {
            try
            {
                var user = await _userService.GetUserById(id);
                if (user == null)
                {
                    return HandleNotFoundException(new KeyNotFoundException(), id.ToString(), "User");
                }
                return Ok(user);
            }
            catch (KeyNotFoundException ex)
            {
                return HandleNotFoundException(ex, id.ToString(), "User");
            }
            catch (Exception ex)
            {
                return HandleException(ex, $"retrieving the user with ID {id}");
            }
        }

        [HttpPost(Name = "Register User")]
        public async Task<ActionResult> Register([FromBody] UserRegistrationModel user)
        {
            try
            {
                await _userService.CreateUser(user);
                return Ok(_localizer["Registered"].Value);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid input for user registration.");
                return BadRequest(new { message = _localizer["InvalidInput"].Value ?? ex.Message });
            }
            catch (Exception ex)
            {
                return HandleException(ex, "registering the user");
            }
        }

        [Authorize(Roles = "Super")]
        [HttpDelete("{id}", Name = "Delete Id")]
        public async Task<ActionResult> Delete(int id)
        {
            try
            {
                await _userService.DeleteUser(id);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return HandleNotFoundException(ex, id.ToString(), "User");
            }
            catch (Exception ex)
            {
                return HandleException(ex, $"deleting the user with ID {id}");
            }
        }

        [Authorize(Roles = "Super")]
        [HttpPost("{id}", Name = "Update User")]
        public async Task<ActionResult> Update(int id, [FromBody] UserUpdateModel updatedUser)
        {
            try
            {
                await _userService.UpdateUser(id, updatedUser);
                return Ok(_localizer["Updated"].Value);
            }
            catch (KeyNotFoundException ex)
            {
                return HandleNotFoundException(ex, id.ToString(), "User");
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid input for user with ID {UserId}.", id);
                return BadRequest(_localizer["ErrorUpdatingUser"].Value ?? "Invalid input for user");
            }
            catch (Exception ex)
            {
                return HandleException(ex, $"updating the user with ID {id}");
            }
        }

        #region new feature

        [Authorize]
        [HttpDelete("Delete", Name = "Delete my user")]
        public async Task<ActionResult> DeleteMyUser()
        {
            try
            {
                if (Request.Cookies.TryGetValue("AuthToken", out var authCookieValue))
                {
                    var claims = await _authService.GetClaims(authCookieValue);
                    var stringId = claims.FirstOrDefault(c => c.Type.Contains("nameidentifier"))?.Value;

                    if (!string.IsNullOrEmpty(stringId))
                    {
                        int id = int.Parse(stringId);
                        await _userService.DeleteUser(id);
                        return Ok(_localizer["DeletedSuccessfully"].Value ?? "Deleted Successfully.");
                    }
                }
                _logger.LogWarning("AuthToken cookie is missing or invalid.");
                return Unauthorized(new { message = _localizer["InvalidAuthToken"].Value });
            }
            catch (Exception ex)
            {
                return HandleException(ex, "deleting the user");
            }
        }

        [Authorize]
        [HttpPost("Update", Name = "Update my user")]
        public async Task<ActionResult> UpdateMyUser([FromBody] UserUpdateModel updateModel)
        {
            try
            {
                if (Request.Cookies.TryGetValue("AuthToken", out var authCookieValue))
                {
                    var claims = await _authService.GetClaims(authCookieValue);
                    var stringId = claims.FirstOrDefault(c => c.Type.Contains("nameidentifier"))?.Value;
                    if (!string.IsNullOrEmpty(stringId))
                    {
                        await _userService.UpdateUser(int.Parse(stringId), updateModel);
                        return Ok();
                    }
                }
                _logger.LogWarning("AuthToken cookie is missing or invalid.");
                return Unauthorized(_localizer["InvalidAuthToken"].Value);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "updating the user");
            }
        }

        [Authorize(Roles = "Admin,Super")]
        [HttpGet("company/{id}", Name = "Get company users")]
        public async Task<ActionResult> GetChilds(int id)
        {
            try
            {
                var childList = await _userService.GetChildren(id);
                if (childList == null)
                {
                    return HandleNotFoundException(new KeyNotFoundException(), id.ToString(), "Company");
                }
                return Ok(childList);
            }
            catch (KeyNotFoundException ex)
            {
                return HandleNotFoundException(ex, id.ToString(), "Company");
            }
            catch (Exception ex)
            {
                return HandleException(ex, $"retrieving the company with ID {id}");
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("Company", Name = "Register company child")]
        public async Task<ActionResult> RegisterChild([FromBody] UserRegistrationModel registrationModel)
        {
            try
            {
                if (Request.Cookies.TryGetValue("AuthToken", out var authCookieValue))
                {
                    var claims = await _authService.GetClaims(authCookieValue);
                    var stringId = claims.FirstOrDefault(c => c.Type.Contains("nameidentifier"))?.Value;
                    if (!string.IsNullOrEmpty(stringId))
                    {
                        await _userService.CreateCompanyUser(int.Parse(stringId), registrationModel);
                        return Ok(_localizer["Registered"].Value ?? "Registered.");
                    }
                }
                _logger.LogWarning("AuthToken cookie is missing or invalid.");
                return Unauthorized(_localizer["InvalidAuthToken"].Value);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "registering the company child");
            }
        }

        [Authorize(Roles = "User")]
        [HttpGet("Company", Name = "Parent company info")]
        public async Task<ActionResult> GetParent()
        {
            try
            {
                if (Request.Cookies.TryGetValue("AuthToken", out var authCookieValue))
                {
                    var claims = await _authService.GetClaims(authCookieValue);
                    var stringId = claims.FirstOrDefault(c => c.Type.Contains("nameidentifier"))?.Value;
                    if (!string.IsNullOrEmpty(stringId))
                    {
                        return Ok(await _userService.GetParent(int.Parse(stringId)));
                    }
                }
                _logger.LogWarning("AuthToken cookie is missing or invalid.");
                return Unauthorized(_localizer["InvalidAuthToken"].Value);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "retrieving parent company info");
            }
        }

        [Authorize]
        [HttpPost("Resend", Name = "Resend activation link")]
        public async Task<ActionResult> Resend()
        {
            try
            {
                if (Request.Cookies.TryGetValue("AuthToken", out var authCookieValue))
                {
                    var claims = await _authService.GetClaims(authCookieValue);
                    var stringId = claims.FirstOrDefault(c => c.Type.Contains("nameidentifier"))?.Value;
                    if (!string.IsNullOrEmpty(stringId))
                    {
                        await _userService.Resend(int.Parse(stringId));
                        return Ok(_localizer["ResendSuccessfull"].Value ?? "Verification link resend successfully");
                    }
                }
                _logger.LogWarning("AuthToken cookie is missing or invalid.");
                return Unauthorized(_localizer["InvalidAuthToken"].Value);
            }
            catch (InvalidOperationException)
            {
                _logger.LogWarning("User already activated");
                return StatusCode(400, _localizer["AlreadyActivated"].Value);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "resending the code");
            }
        }

        #endregion
    }
}
