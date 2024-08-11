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
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during login for email: {Email}", userLoginModel.Email);
                throw new Exception("An error occurred during the login process. Please try again later.", ex);
            }
        }

        public async Task<IEnumerable<Claim>> GetClaims(string token)
        {
            if (token == null)
            {
                throw new InvalidDataException();
            }
            try
            {
                var claims = _tokenService.GetTokenClaims(token);
                if (claims == null || !claims.Any())
                {

                }
                return claims;
            }
            catch (Exception)
            {
                throw;
            }


        }


    }
}
