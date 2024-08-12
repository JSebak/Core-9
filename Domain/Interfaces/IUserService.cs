using Domain.Models;

namespace Domain.Interfaces
{
    public interface IUserService
    {
        Task<IEnumerable<UserDto>> GetAllUsers();
        Task<UserDetailsDto> GetUserById(int id);
        Task<UserDetailsDto> GetUserByEmail(string email);
        Task CreateUser(UserRegistrationModel user);
        Task UpdateUser(int id, UserUpdateModel updatedUser);
        Task DeleteUser(int id);

        Task<IEnumerable<UserDetailsDto>> GetChildren(int userId);
        Task CreateCompanyUser(int parentId, UserRegistrationModel updateModel);
        Task<UserDetailsDto> GetParent(int userId);
        Task ChangeActivation(int id, bool active);
        Task Resend(int userId);
    }
}
