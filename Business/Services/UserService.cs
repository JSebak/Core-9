using AutoMapper;
using Business.Interfaces;
using Domain.Entities;
using Domain.Interfaces;
using Domain.Models;
using Microsoft.Extensions.Logging;

namespace Business.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IEmailService _mailService;
        private readonly ITokenService _tokenService;
        private readonly ILogger<UserService> _logger;
        private readonly IMapper _mapper;

        public UserService(IUserRepository userRepository, IEmailService mailService, ITokenService tokenService, ILogger<UserService> logger, IMapper mapper)
        {
            _userRepository = userRepository;
            _mailService = mailService;
            _tokenService = tokenService;
            _logger = logger;
            _mapper = mapper;
        }

        public async Task<IEnumerable<UserDto>> GetAllUsers()
        {
            try
            {
                return (await _userRepository.GetAll()).Select(user => new UserDto { Id = user.Id, Username = user.Username });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching all users.");
                throw new Exception("An error occurred while fetching all users.", ex);
            }
        }

        public async Task<UserDetailsDto> GetUserById(int id)
        {
            try
            {
                var user = await _userRepository.GetById(id);
                if (user == null)
                    throw new KeyNotFoundException("User not found");
                return _mapper.Map<UserDetailsDto>(user);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "User with ID {UserId} not found.", id);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching the user with ID {UserId}.", id);
                throw new Exception("An error occurred while fetching the user.", ex);
            }
        }

        public async Task<UserDetailsDto> GetUserByEmail(string email)
        {
            try
            {
                var user = await _userRepository.GetByEmail(email);
                if (user == null)
                    throw new KeyNotFoundException("User not found");
                return _mapper.Map<UserDetailsDto>(user);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "User with Email {UserEmail} not found.", email);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching the user with Email {UserEmail}.", email);
                throw new Exception("An error occurred while fetching the user.", ex);
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
                var token = _tokenService.GenerateToken(user);
                var verificationLink = $"https://localhost:7279/Auth/verify?token={token}";
                await _mailService.SendEmailAsync(user.Email, "Account Verification", verificationLink);
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
                if (!string.IsNullOrEmpty(updatedUser.Role))
                {
                    var updatedUserRole = User.ConvertToUserRole(updatedUser.Role);
                    if (user.Role != updatedUserRole)
                    {
                        user.ChangeRole(updatedUserRole);
                        isUpdated = true;
                    }
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
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "User with ID {UserId} not found.", id);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while deleting the user with ID {UserId}.", id);
                throw new Exception("An error occurred while deleting the user.", ex);
            }
        }

        public async Task<IEnumerable<UserDetailsDto>> GetChildren(int userId)
        {
            try
            {
                var list = (await _userRepository.GetChildren(userId)).Select(_mapper.Map<UserDetailsDto>);
                return list;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching the children for user with ID {UserId}.", userId);
                throw new Exception("An error occurred while fetching the children.", ex);
            }
        }

        public async Task CreateCompanyUser(int parentId, UserRegistrationModel userModel)
        {
            if (string.IsNullOrWhiteSpace(userModel.UserName) ||
                string.IsNullOrWhiteSpace(userModel.Email) ||
                string.IsNullOrWhiteSpace(userModel.Password) ||
                string.IsNullOrWhiteSpace(userModel.Role))
            {
                throw new ArgumentException("All fields are required");
            }

            try
            {
                var existingUser = await _userRepository.GetByEmail(userModel.Email);
                var existingUserByUsername = await _userRepository.GetByUserName(userModel.UserName);

                if (existingUser != null || existingUserByUsername != null)
                    throw new InvalidDataException("User already exists");

                var user = new User(userModel.UserName, userModel.Password, userModel.Email, User.ConvertToUserRole(userModel.Role), parentId);
                var hashedPassword = BCrypt.Net.BCrypt.HashPassword(userModel.Password);
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
                var message = $"An error occurred while registering the user with Email {userModel.Email}.";
                _logger.LogError(ex, message);
                throw new Exception(message, ex);
            }
        }

        public async Task ChangeActivation(int id, bool active)
        {
            try
            {
                var user = await _userRepository.GetById(id);
                if (user == null)

                    throw new KeyNotFoundException("User not found");

                if (user.Active != active)
                {
                    if (!user.Active)
                    {
                        user.ActivateUser();
                    }
                    else
                    {
                        user.DeactivateUser();
                    }
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

        public async Task<UserDetailsDto> GetParent(int userId)
        {
            try
            {
                var parent = _mapper.Map<UserDetailsDto>(await _userRepository.GetParent(userId));
                if (parent == null)
                    throw new KeyNotFoundException("Parent user not found");

                return parent;
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Parent user for User with ID {UserId} not found.", userId);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching the parent for user with ID {UserId}.", userId);
                throw new Exception("An error occurred while fetching the parent.", ex);
            }
        }

        public async Task Resend(int userId)
        {
            try
            {
                var user = await _userRepository.GetById(userId);

                if (user == null)
                    throw new InvalidDataException("User not found");
                if (user.Active) throw new InvalidOperationException("User already activated");
                var token = _tokenService.GenerateToken(user);
                var verificationLink = $"https://localhost:7279/Auth/verify?token={token}";
                await _mailService.SendEmailAsync(user.Email, "Account Verification", verificationLink);
            }
            catch (InvalidDataException)
            {
                throw;
            }
        }
    }
}