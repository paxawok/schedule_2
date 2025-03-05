namespace schedule_2.Models
{
    public class Teacher
    {
        public int Id { get; set; }
        public required string FirstName { get; set; }
        public required string LastName { get; set; }
        public required string Email { get; set; }
        public virtual ICollection<Event> Events { get; set; } = new List<Event>();
        public virtual ICollection<CourseTeacher> CourseTeachers { get; set; } = new List<CourseTeacher>(); // M:M з Course
    }
}