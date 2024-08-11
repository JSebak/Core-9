using Domain.Entities;
using Microsoft.EntityFrameworkCore;
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
            // Ensure the database is created.
            _context.Database.EnsureCreated();

            // Check if there are any users in the database. If yes, exit early.
            if (_context.Users.Any())
            {
                return; // No need to seed if data already exists.
            }

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    var users = new List<User>
                    {
                        new("SuperAdmin", HashPassword("Admin123!"), "superadmin@core.com", Domain.Enums.UserRole.Super, null, true),
                        new("Admin", HashPassword("Admin123!"), "admin@core.com", Domain.Enums.UserRole.Admin, null, true),
                        new("Guest", HashPassword("Admin123!"), "guest@core.com", Domain.Enums.UserRole.User, null, true),
                        new("Company1", HashPassword("Admin123!"), "company1@core.com", Domain.Enums.UserRole.Admin, null, true),
                    };

                    await _context.ResetPrimaryKeyAutoIncrementAsync();

                    await _context.Users.AddRangeAsync(users);
                    await _context.SaveChangesAsync();

                    var company1 = await _context.Users.FirstOrDefaultAsync(u => u.Username == "Company1");
                    if (company1 != null)
                    {
                        var employee = new User(
                            username: "Employee1",
                            password: HashPassword("Admin123!"),
                            email: "employee1@core.com",
                            role: Domain.Enums.UserRole.User,
                            parentUserId: company1.Id,
                            active: true
                        );

                        _context.Users.Add(employee);
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

        private string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }
    }
}
