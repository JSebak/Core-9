using AutoMapper;
using Business.Interfaces;
using Domain.Entities;
using Domain.Interfaces;
using Domain.Models;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace Business.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserService _userService;
        private readonly ITokenService _tokenService;
        private readonly ILogger<AuthService> _logger;
        private readonly IMapper _mapper;

        public AuthService(IUserService userService, ITokenService tokenService, IMapper mapper, ILogger<AuthService> logger)
        {
            _userService = userService;
            _tokenService = tokenService;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<string?> Login(UserLoginModel userLoginModel)
        {
            if (userLoginModel == null || string.IsNullOrWhiteSpace(userLoginModel.Email) || string.IsNullOrWhiteSpace(userLoginModel.Password))
            {
                _logger.LogWarning("Login attempt with missing or invalid credentials.");
                throw new ArgumentException("Email and Password are required.");
            }

            try
            {
                var user = _mapper.Map<User>(await _userService.GetUserByEmail(userLoginModel.Email));

                if (user == null)
                {
                    _logger.LogWarning("Login attempt with invalid email: {Email}", userLoginModel.Email);
                    throw new UnauthorizedAccessException("Invalid Email or Password");
                }

                if (BCrypt.Net.BCrypt.Verify(userLoginModel.Password, user.Password))
                {
                    var token = _tokenService.GenerateToken(user);
                    return token;
                }
                else
                {
                    _logger.LogWarning("Invalid password attempt for user with email: {Email}", userLoginModel.Email);
                    throw new UnauthorizedAccessException("Invalid Email or Password");
                }
            }
            catch (UnauthorizedAccessException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during login for email: {Email}", userLoginModel.Email);
                throw new Exception("An error occurred during the login process. Please try again later.", ex);
            }
        }

        public async Task<IEnumerable<Claim>> GetClaims(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                _logger.LogWarning("Invalid token provided.");
                throw new ArgumentException("Token cannot be null or empty.");
            }

            try
            {
                var claims = _tokenService.GetTokenClaims(token);
                if (claims == null || !claims.Any())
                {
                    _logger.LogWarning("No claims found for token: {Token}", token);
                    throw new UnauthorizedAccessException("Invalid or expired token.");
                }

                return claims;
            }
            catch (UnauthorizedAccessException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving claims for token: {Token}", token);
                throw new Exception("An error occurred while retrieving claims. Please try again later.", ex);
            }
        }

        public async Task VerifyAccount(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                _logger.LogWarning("Invalid token provided for account verification.");
                throw new ArgumentException("Token cannot be null or empty.");
            }

            try
            {
                if (!_tokenService.ValidateToken(token))
                {
                    _logger.LogWarning("Token validation failed for token: {Token}", token);
                    throw new UnauthorizedAccessException("Invalid or expired token.");
                }

                var claims = await GetClaims(token);
                var id = claims.FirstOrDefault(c => c.Type.Contains("nameidentifier"))?.Value;

                if (string.IsNullOrEmpty(id))
                {
                    _logger.LogWarning("No user ID found in token claims.");
                    throw new UnauthorizedAccessException("Invalid token claims.");
                }

                var user = _mapper.Map<User>(await _userService.GetUserById(int.Parse(id)));
                if (user == null)
                {
                    _logger.LogWarning("No user found with ID: {UserId}", id);
                    throw new KeyNotFoundException("User not found.");
                }

                user.ActivateUser();
                await _userService.ChangeActivation(user.Id, true);
            }
            catch (UnauthorizedAccessException)
            {
                throw;
            }
            catch (KeyNotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while verifying the account with token: {Token}", token);
                throw new Exception("An error occurred during account verification. Please try again later.", ex);
            }
        }
    }
}
