namespace schedule_2.Models
{
    public class Teacher
    {
        public int Id { get; set; }
        public required string FirstName { get; set; }
        public required string LastName { get; set; }
        public required string Email { get; set; }
        public virtual ICollection<Event> Events { get; set; } = new List<Event>();
        public virtual ICollection<Course> Courses { get; set; } = new List<Course>();
    }
}