using AutoMapper;
using Domain.Entities;
using Domain.Interfaces;
using Domain.Models;
using Microsoft.Extensions.Logging;

namespace Business.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly ILogger<UserService> _logger;
        private readonly IMapper _mapper;

        public UserService(IUserRepository userRepository, ILogger<UserService> logger, IMapper mapper)
        {
            _userRepository = userRepository;
            _logger = logger;
            _mapper = mapper;
        }

        public async Task<IEnumerable<UserDto>> GetAllUsers()
        {
            try
            {
                return (await _userRepository.GetAll()).Select(user => new UserDto { Id = user.Id, Username = user.Username });
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<UserDetailsDto> GetUserById(int id)
        {
            try
            {
                return _mapper.Map<UserDetailsDto>(await _userRepository.GetById(id));
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<UserDetailsDto> GetUserByEmail(string email)
        {
            try
            {
                return _mapper.Map<UserDetailsDto>(await _userRepository.GetByEmail(email));
            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task CreateUser(UserRegistrationModel userToRegister)
        {
            if (string.IsNullOrWhiteSpace(userToRegister.UserName) ||
                string.IsNullOrWhiteSpace(userToRegister.Email) ||
                string.IsNullOrWhiteSpace(userToRegister.Password) ||
                string.IsNullOrWhiteSpace(userToRegister.Role))
            {
                throw new ArgumentException("All fields are required");
            }

            try
            {
                var existingUser = await _userRepository.GetByEmail(userToRegister.Email);
                var existingUserByUsername = await _userRepository.GetByUserName(userToRegister.UserName);

                if (existingUser != null || existingUserByUsername != null)
                    throw new InvalidDataException("User already exists");

                var user = new User(userToRegister.UserName, userToRegister.Password, userToRegister.Email, User.ConvertToUserRole(userToRegister.Role));
                var hashedPassword = BCrypt.Net.BCrypt.HashPassword(userToRegister.Password);
                user.UpdatePassword(hashedPassword);

                await _userRepository.Add(user);
            }
            catch (InvalidDataException)
            {
                throw;
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (Exception ex)
            {
                var message = $"An error occurred while registering the user with Email {userToRegister.Email}.";
                _logger.LogError(ex, message);
                throw new Exception(message, ex);
            }
        }

        public async Task UpdateUser(int id, UserUpdateModel updatedUser)
        {
            try
            {
                var user = await _userRepository.GetById(id);
                if (user == null)
                    throw new KeyNotFoundException("User not found");

                var isUpdated = false;
                var updatedUserRole = User.ConvertToUserRole(updatedUser.Role);
                if (!string.IsNullOrEmpty(updatedUser.Email) && user.Email != updatedUser.Email)
                {
                    user.UpdateEmail(updatedUser.Email);
                    isUpdated = true;
                }
                if (!string.IsNullOrEmpty(updatedUser.UserName) && user.Username != updatedUser.UserName)
                {
                    user.UpdateUserName(updatedUser.UserName);
                    isUpdated = true;
                }
                if (!string.IsNullOrEmpty(updatedUser.Password) && !BCrypt.Net.BCrypt.Verify(updatedUser.Password, user.Password))
                {
                    var hashedPassword = BCrypt.Net.BCrypt.HashPassword(updatedUser.Password);
                    user.UpdatePassword(hashedPassword);
                    isUpdated = true;
                }
                if (!string.IsNullOrEmpty(updatedUser.Role) && user.Role != updatedUserRole)
                {
                    user.ChangeRole(updatedUserRole);
                    isUpdated = true;
                }

                if (isUpdated)
                {
                    await _userRepository.Update(user);
                }
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "User with ID {UserId} not found.", id);
                throw;
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while updating the user with ID {UserId}.", id);
                throw new Exception("An error occurred while updating the user.", ex);
            }
        }

        public async Task DeleteUser(int id)
        {
            try
            {
                var user = await _userRepository.GetById(id);
                if (user == null)
                    throw new KeyNotFoundException("User not found");
                await _userRepository.Delete(id);
            }
            catch (Exception)
            {

                throw;
            }

        }
    }
}
