using Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Data
{
    public class DataSeeder
    {
        private readonly UserDbContext _context;
        private readonly ILogger<DataSeeder> _logger;

        public DataSeeder(UserDbContext context, ILogger<DataSeeder> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task Seed()
        {
            _context.Database.EnsureCreated();
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    if (!_context.Users.Any())
                    {
                        var users = new List<User>
                        {
                            new User(username: "SuperAdmin", password: "Admin.123!", email: "superadmin@admin.com", role: Domain.Enums.UserRole.Super),
                            new User(username: "Admin", password: "Admin.123!", email: "admin@admin.com", role: Domain.Enums.UserRole.Admin),
                            new User(username: "Guest", password: "Admin.123!", email: "guest@admin.com", role: Domain.Enums.UserRole.User),
                        };

                        users.ForEach(user =>
                        {
                            user.UpdatePassword(BCrypt.Net.BCrypt.HashPassword(user.Password));
                        });

                        await _context.ResetPrimaryKeyAutoIncrementAsync();
                        _context.Users.AddRange(users);
                        await _context.SaveChangesAsync();
                    }

                    await transaction.CommitAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred while seeding the database.");
                    await transaction.RollbackAsync();

                    throw new Exception("Database seeding failed. The transaction has been rolled back.", ex);
                }
            }
        }
    }
}
