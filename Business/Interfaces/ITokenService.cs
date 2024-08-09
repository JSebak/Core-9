using Domain.Entities;

namespace Business.Interfaces
{
    public interface ITokenService
    {
        public string GenerateToken(User user);
    }
}
