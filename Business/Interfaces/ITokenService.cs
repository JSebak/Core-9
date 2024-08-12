using Domain.Entities;
using System.Security.Claims;

namespace Business.Interfaces
{
    public interface ITokenService
    {
        public string GenerateToken(User user);
        IEnumerable<Claim> GetTokenClaims(string token);
        bool ValidateToken(string token);
    }
}
