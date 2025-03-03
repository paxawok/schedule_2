namespace schedule_2.Models
{
    public class Course
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public virtual ICollection<Event> Events { get; set; } = new List<Event>();
        public virtual ICollection<Teacher> Teachers { get; set; } = new List<Teacher>();
        public virtual ICollection<Group> Groups { get; set; } = new List<Group>();
    }
}