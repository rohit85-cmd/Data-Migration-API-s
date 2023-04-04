using CSVREADER.Models;
using Microsoft.EntityFrameworkCore;

namespace CSVREADER.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }
        public DbSet<Staff> StaffData
        {
            get; set;
        }

    }
}
