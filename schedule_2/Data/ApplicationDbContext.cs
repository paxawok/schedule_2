using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace schedule_2.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {

        }
        // DbSet'и для майбутніх моделей
        //public DbSet<Classroom> Classrooms { get; set; }
        //public DbSet<Course> Courses { get; set; }
        //public DbSet<Group> Groups { get; set; }
    }
}
