using CSVREADER.Models;
using Microsoft.EntityFrameworkCore;

namespace CSVREADER.Data
{
    //DbContext is like database and DbSets are like tables in that database
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }
        public DbSet<Staff> StaffData
        {
            get; set;
        }

    }
}
