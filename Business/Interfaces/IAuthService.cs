using Domain.Models;
using System.Security.Claims;

namespace Business.Interfaces
{
    public interface IAuthService
    {
        Task<string?> Login(UserLoginModel userLoginModel);

        Task<IEnumerable<Claim>> GetClaims(string token);
    }
}
