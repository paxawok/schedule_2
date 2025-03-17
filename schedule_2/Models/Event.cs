namespace schedule_2.Models
{
    public class Event
    {
        public int Id { get; set; }
        public required string Title { get; set; }
        public DateTime StartDateTime { get; set; }
        public DateTime EndDateTime { get; set; }
        public bool IsRecurring { get; set; }
        public string? RecurrencePattern { get; set; }

        // Foreign keys
        public int TeacherId { get; set; }
        public int CourseId { get; set; }
        public int ClassroomId { get; set; }
        public int ScheduleId { get; set; }

        // Navigation properties
        public Teacher Teacher { get; set; } = null!;
        public Course Course { get; set; } = null!;
        public Classroom Classroom { get; set; } = null!;
        public Schedule Schedule { get; set; } = null!;

        // Many-to-many with Group
        public virtual ICollection<EventGroup> EventGroups { get; set; } = new List<EventGroup>();
        // Many-to-many with Subgroup (через SubgroupEvent)
        public virtual ICollection<SubgroupEvent> SubgroupEvents { get; set; } = new List<SubgroupEvent>();
    }
}
