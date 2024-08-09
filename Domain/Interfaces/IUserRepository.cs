using Domain.Entities;

namespace Domain.Interfaces
{
    public interface IUserRepository
    {
        Task<IEnumerable<User>> GetAll();
        Task<User?> GetById(int id);
        Task<User?> GetByEmail(string email);
        Task<User?> GetByUserName(string userName);
        Task Add(User user);
        Task Update(User user);
        Task Delete(int id);
    }
}
