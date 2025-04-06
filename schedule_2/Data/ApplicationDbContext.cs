using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using schedule_2.Models;

namespace schedule_2.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public DbSet<Classroom> Classrooms { get; set; }
        public DbSet<Course> Courses { get; set; }
        public DbSet<Event> Events { get; set; }
        public DbSet<Group> Groups { get; set; }
        public DbSet<Schedule> Schedules { get; set; }
        public DbSet<Teacher> Teachers { get; set; }
        public DbSet<CourseGroup> CourseGroups { get; set; }
        public DbSet<CourseTeacher> CourseTeachers { get; set; }
        public DbSet<EventGroup> EventGroups { get; set; }
        public DbSet<Subgroup> Subgroups { get; set; }
        public DbSet<SubgroupCourse> SubgroupCourses { get; set; }
        public DbSet<SubgroupEvent> SubgroupEvents { get; set; }
        public DbSet<Student> Students { get; set; }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Налаштування зв’язку M:M між Course і Teacher через CourseTeacher
            modelBuilder.Entity<CourseTeacher>()
                .HasKey(ct => new { ct.CourseId, ct.TeacherId });

            modelBuilder.Entity<CourseTeacher>()
                .HasOne(ct => ct.Course)
                .WithMany(c => c.CourseTeachers)
                .HasForeignKey(ct => ct.CourseId);

            modelBuilder.Entity<CourseTeacher>()
                .HasOne(ct => ct.Teacher)
                .WithMany(t => t.CourseTeachers)
                .HasForeignKey(ct => ct.TeacherId);

            // Налаштування зв’язку M:M між Course і Group через CourseGroup
            modelBuilder.Entity<CourseGroup>()
                .HasKey(cg => new { cg.CourseId, cg.GroupId });

            modelBuilder.Entity<CourseGroup>()
                .HasOne(cg => cg.Course)
                .WithMany(c => c.CourseGroups)
                .HasForeignKey(cg => cg.CourseId);

            modelBuilder.Entity<CourseGroup>()
                .HasOne(cg => cg.Group)
                .WithMany(g => g.CourseGroups)
                .HasForeignKey(cg => cg.GroupId);

            // Налаштування зв’язку M:M між Event і Group через EventGroup
            modelBuilder.Entity<EventGroup>()
                .HasKey(eg => new { eg.EventId, eg.GroupId });

            modelBuilder.Entity<EventGroup>()
                .HasOne(eg => eg.Event)
                .WithMany(e => e.EventGroups)
                .HasForeignKey(eg => eg.EventId);

            modelBuilder.Entity<EventGroup>()
                .HasOne(eg => eg.Group)
                .WithMany(g => g.EventGroups)
                .HasForeignKey(eg => eg.GroupId);

            // Налаштування зв’язку 1:M між Group і Subgroup
            modelBuilder.Entity<Subgroup>()
                .HasKey(s => s.Id);

            modelBuilder.Entity<Subgroup>()
                .HasOne(s => s.Group)
                .WithMany(g => g.Subgroups)
                .HasForeignKey(s => s.GroupId);

            // Налаштування зв’язку M:M між Subgroup і Course
            modelBuilder.Entity<SubgroupCourse>()
                .HasKey(sc => new { sc.SubgroupId, sc.CourseId });

            modelBuilder.Entity<SubgroupCourse>()
                .HasOne(sc => sc.Subgroup)
                .WithMany(s => s.SubgroupCourses)
                .HasForeignKey(sc => sc.SubgroupId);

            modelBuilder.Entity<SubgroupCourse>()
                .HasOne(sc => sc.Course)
                .WithMany(c => c.SubgroupCourses)
                .HasForeignKey(sc => sc.CourseId);

            // Налаштування зв’язку M:M між Subgroup і Event
            modelBuilder.Entity<SubgroupEvent>()
                .HasKey(se => new { se.SubgroupId, se.EventId });

            modelBuilder.Entity<SubgroupEvent>()
                .HasOne(se => se.Subgroup)
                .WithMany(s => s.SubgroupEvents)
                .HasForeignKey(se => se.SubgroupId);

            modelBuilder.Entity<SubgroupEvent>()
                .HasOne(se => se.Event)
                .WithMany(e => e.SubgroupEvents)
                .HasForeignKey(se => se.EventId);

            // Налаштування зв’язку між Teacher i IdentityUser
            modelBuilder.Entity<Teacher>()
                .HasOne(t => t.User)
                .WithOne()
                .HasForeignKey<Teacher>(t => t.UserId);

            modelBuilder.Entity<Student>()
                .HasOne(s => s.Group)
                .WithMany()
                .HasForeignKey(s => s.GroupId)
                .OnDelete(DeleteBehavior.Restrict);

            // Налаштування зв'язку між Student i IdentityUser
            modelBuilder.Entity<Student>()
                .HasOne(s => s.User)
                .WithOne()
                .HasForeignKey<Student>(s => s.UserId);
        }
    }
}
