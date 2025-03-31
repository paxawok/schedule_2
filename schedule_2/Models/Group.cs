using System.ComponentModel.DataAnnotations;

namespace schedule_2.Models
{
    public class Group
    {
        public int Id { get; set; }
        [StringLength(12, ErrorMessage = "The group name cannot exceed 12 characters")]
        public required string Name { get; set; }
        public virtual ICollection<EventGroup> EventGroups { get; set; } = new List<EventGroup>(); // M:M з Event
        public virtual ICollection<CourseGroup> CourseGroups { get; set; } = new List<CourseGroup>(); // M:M з Course
        public virtual ICollection<Subgroup> Subgroups { get; set; } = new List<Subgroup>(); // 1:M з підгрупами
    }
}
