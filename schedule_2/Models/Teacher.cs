using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace schedule_2.Models
{
    public class Teacher
    {
        public int Id { get; set; }
        public required string FirstName { get; set; }
        public required string LastName { get; set; }
        [RegularExpression(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$",
            ErrorMessage = "Please enter a valid email address")]
        public required string Email { get; set; }
        // зв'язок з identity
        public string? UserId { get; set; }
        public IdentityUser? User { get; set; }
        public virtual ICollection<Event> Events { get; set; } = new List<Event>();
        public virtual ICollection<CourseTeacher> CourseTeachers { get; set; } = new List<CourseTeacher>(); // M:M з Course
    }
}