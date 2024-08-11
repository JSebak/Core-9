using Domain.Entities;
using Domain.Enums;
using Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly UserDbContext _context;
        private readonly ILogger<UserRepository> _logger;

        public UserRepository(UserDbContext context, ILogger<UserRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<User>> GetAll()
        {
            try
            {
                return await _context.Users.ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving all users.");
                throw new Exception("An error occurred while retrieving all users.", ex);
            }
        }

        public async Task Add(User user)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            try
            {
                if (_context.Users.Any(u => u.Email == user.Email))
                    throw new InvalidOperationException("User with the same email already exists");

                _context.Users.Add(user);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "An error occurred while adding the user with Email {Email}.", user.Email);
                throw new Exception("A database error occurred while adding the user.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while adding the user with Email {Email}.", user.Email);
                throw new Exception("An unexpected error occurred while adding the user.", ex);
            }
        }

        public async Task Delete(int id)
        {
            if (id <= 0)
                throw new ArgumentException("Invalid user ID", nameof(id));

            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                    throw new KeyNotFoundException("User not found");

                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "An error occurred while deleting the user with ID {Id}.", id);
                throw new Exception("A database error occurred while deleting the user.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while deleting the user with ID {Id}.", id);
                throw new Exception("An unexpected error occurred while deleting the user.", ex);
            }
        }

        public async Task<User?> GetById(int id)
        {
            if (id <= 0)
                throw new ArgumentException("Invalid user ID", nameof(id));

            try
            {
                var user = await _context.Users.FindAsync(id);
                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving the user with ID {Id}.", id);
                throw new Exception("An error occurred while retrieving the user.", ex);
            }
        }

        public async Task Update(User user)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            try
            {
                if (_context.Users.Any(u => u.Id == user.Id))
                {
                    _context.Users.Update(user);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    throw new KeyNotFoundException("User not found");
                }
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "An error occurred while updating the user with ID {Id}.", user.Id);
                throw new Exception("A database error occurred while updating the user.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while updating the user with ID {Id}.", user.Id);
                throw new Exception("An unexpected error occurred while updating the user.", ex);
            }
        }

        public async Task<User?> GetByEmail(string email)
        {
            if (string.IsNullOrEmpty(email))
                throw new ArgumentException("Invalid user email", nameof(email));

            try
            {
                var user = await _context.Users.SingleOrDefaultAsync(u => u.Email == email);
                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving the user with Email {Email}.", email);
                throw new Exception("An error occurred while retrieving the user.", ex);
            }
        }

        public async Task<User?> GetByUserName(string userName)
        {
            if (string.IsNullOrEmpty(userName))
                throw new ArgumentException("Invalid username", nameof(userName));

            try
            {
                var user = await _context.Users.SingleOrDefaultAsync(u => u.Username == userName);
                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving the user with Username {UserName}.", userName);
                throw new Exception("An error occurred while retrieving the user.", ex);
            }
        }

        public async Task<IEnumerable<User>> GetChildren(int userId)
        {
            try
            {
                return await _context.Users.Where(u => u.ParentUserId == userId).ToListAsync();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<IEnumerable<User>> GetCompanyAdmins()
        {
            try
            {
                var admins = await _context.Users.Where(u => u.Role == UserRole.Admin).ToListAsync();
                return admins;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<User> GetParent(int id)
        {
            if (id <= 0)
                throw new ArgumentException("Invalid user id");

            try
            {
                var child = await GetById(id);
                if (child == null)
                {
                    throw new Exception();
                }
                else
                {
                    if (child.ParentUserId == null)
                    {
                        return null;
                    }
                    return await GetById(child.ParentUserId.Value);
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving the parent for the user with id {id}.", id);
                throw new Exception("An error occurred while retrieving the user.", ex);
            }
        }
    }
}
