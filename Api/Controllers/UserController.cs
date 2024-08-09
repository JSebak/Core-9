using Domain.Interfaces;
using Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ILogger<UserController> _logger;
        private readonly IStringLocalizer<UserController> _localizer;

        public UserController(IUserService userService, ILogger<UserController> logger, IStringLocalizer<UserController> localizer)
        {
            _userService = userService;
            _logger = logger;
            _localizer = localizer;
        }
        [Authorize(Roles = "Admin,Super")]
        [HttpGet(Name = "Get Users", Order = 1)]
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
        [HttpGet("{id}", Name = "Get By Id", Order = 2)]
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

        [HttpPost(Name = "Register User", Order = 3)]
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

        [Authorize(Roles = "Admin,Super")]
        [HttpDelete("{id}", Name = "Delete Id", Order = 5)]
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

        [Authorize(Roles = "admin,super")]
        [HttpPost("{id}", Name = "Update User", Order = 4)]
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
    }
}
