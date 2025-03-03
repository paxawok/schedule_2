namespace schedule_2.Models
{
    public class Event
    {
        public int Id { get; set; }
        public string Title { get; set; }
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
        public virtual ICollection<Group> Groups { get; set; } = new List<Group>();
    }
}
