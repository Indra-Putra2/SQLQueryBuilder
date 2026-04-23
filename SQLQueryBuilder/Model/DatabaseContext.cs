using Microsoft.EntityFrameworkCore;

namespace SQLQueryBuilder.Model
{
    public class DatabaseContext : DbContext
    {
        public DbSet<RamData> RamData { get; set; }
        public DbSet<Brand> Brands { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Replace with your actual connection string
            optionsBuilder.UseSqlServer(@"Server=YOUR_SERVER;Database=YOUR_DB;Trusted_Connection=True;");
        }
    }
}
