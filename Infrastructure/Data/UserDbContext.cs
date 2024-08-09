using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data
{
    public class UserDbContext : DbContext
    {
        public UserDbContext(DbContextOptions<UserDbContext> options) : base(options) { }
        public DbSet<User> Users { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }

        public async Task ResetPrimaryKeyAutoIncrementAsync()
        {
            await Database.ExecuteSqlRawAsync("DELETE FROM sqlite_sequence WHERE name = 'Users';");
        }
    }

}
