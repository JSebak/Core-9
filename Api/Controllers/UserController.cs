using Business.Interfaces;
using Domain.Interfaces;
using Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

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
                _logger.LogError(ex, "An error occurred while retrieving users.");
                return StatusCode(500, new { message = _localizer["ErrorRetrievingUsers"].Value });
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
                    _logger.LogWarning("User with ID {UserId} not found.", id);
                    return NotFound(new { message = _localizer["UserNotFound"].Value });
                }
                return Ok(user);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "User with ID {UserId} not found.", id);
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving the user with ID {UserId}.", id);
                return StatusCode(500, new { message = _localizer["ErrorRetrievingUser"].Value });
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
                return BadRequest(_localizer["Registered"].Value ?? ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while registering the user.");
                return StatusCode(500, _localizer["ErrorRegisteringUser"].Value ?? "An error occurred while registering the user.");
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
                _logger.LogWarning(ex, "User with ID {UserId} not found.", id);
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while deleting the user with ID {UserId}.", id);
                return StatusCode(500, new { message = _localizer["ErrorDeletingUser"].Value });
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
                _logger.LogWarning(ex, "User with ID {UserId} not found.", id);
                return NotFound(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid input for user with ID {UserId}.", id);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while updating the user with ID {UserId}.", id);
                return StatusCode(500, new { message = _localizer["ErrorUpdatingUser"].Value });
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
                        await _userService.DeleteUser(int.Parse(stringId));
                    }
                    return Ok();
                }
                else
                {
                    //_logger.LogWarning("Company with ID {UserId} not found.", id);
                    return StatusCode(401, _localizer["ErrorRetrievingUser"].Value);
                }
            }
            catch (KeyNotFoundException ex)
            {
                //_logger.LogWarning(ex, "Company with ID {UserId} not found.", id);
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                //_logger.LogError(ex, "An error occurred while retrieving the company with ID {UserId}.", id);
                return StatusCode(500, _localizer["ErrorRetrievingUser"].Value);
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
                    }
                    return Ok();
                }
                else
                {
                    //_logger.LogWarning("Company with ID {UserId} not found.", id);
                    return StatusCode(401, _localizer["ErrorRetrievingUser"].Value);
                }
            }
            catch (KeyNotFoundException ex)
            {
                //_logger.LogWarning(ex, "Company with ID {UserId} not found.", id);
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                //_logger.LogError(ex, "An error occurred while retrieving the company with ID {UserId}.", id);
                return StatusCode(500, _localizer["ErrorRetrievingUser"].Value);
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
                    _logger.LogWarning("Company with ID {UserId} not found.", id);
                    return NotFound(new { message = _localizer["UserNotFound"].Value });
                }
                return Ok(childList);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Company with ID {UserId} not found.", id);
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving the company with ID {UserId}.", id);
                return StatusCode(500, new { message = _localizer["ErrorRetrievingUser"].Value });
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
                    }
                    return Ok();
                }
                else
                {
                    throw new Exception();
                }
            }
            //catch (KeyNotFoundException ex)
            //{
            //    //_logger.LogWarning(ex, "Company with ID {UserId} not found.", id);
            //    return NotFound(new { message = ex.Message });
            //}
            catch (Exception ex)
            {
                //_logger.LogError(ex, "An error occurred while retrieving the company with ID {UserId}.", id);
                return StatusCode(500, new { message = _localizer["ErrorRetrievingUser"].Value });
            }
        }

        [Authorize(Roles = "User")]
        [HttpGet("Company", Name = "Parent company info ")]
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
                    else
                    {
                        throw new Exception();
                    }

                }
                else
                {
                    throw new Exception();
                }
            }
            //catch (KeyNotFoundException ex)
            //{
            //    //_logger.LogWarning(ex, "Company with ID {UserId} not found.", id);
            //    return NotFound(new { message = ex.Message });
            //}
            catch (Exception ex)
            {
                //_logger.LogError(ex, "An error occurred while retrieving the company with ID {UserId}.", id);
                return StatusCode(500, new { message = _localizer["ErrorRetrievingUser"].Value });
            }
        }


        #endregion
    }
}
