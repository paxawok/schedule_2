namespace schedule_2.Models
{
    public class Group
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public virtual ICollection<Event> Events { get; set; } = new List<Event>();
        public virtual ICollection<Course> Courses { get; set; } = new List<Course>();
    }
}
