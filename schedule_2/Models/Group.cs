namespace schedule_2.Models
{
    public class Group
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public virtual ICollection<EventGroup> EventGroups { get; set; } = new List<EventGroup>(); // M:M з Event
        public virtual ICollection<CourseGroup> CourseGroups { get; set; } = new List<CourseGroup>(); // M:M з Course
        public virtual ICollection<Subgroup> Subgroups { get; set; } = new List<Subgroup>(); // 1:M з підгрупами
    }
}
