//namespace schedule_2.Models
//{
//    public class Schedule
//    {
//        public int Id { get; set; }
//        public int GroupId { get; set; }
//        public int TeacherId { get; set; }
//        public int CourseId { get; set; }
//        public int ClassroomId { get; set; }
//        public DateTime StartTime { get; set; }
//        public DateTime EndTime { get; set; }
//        public DayOfWeek DayOfWeek { get; set; }
//    }
//}



namespace schedule_2.Models
{
    public class Schedule
    {
        public int Id { get; set; }

        // Foreign Keys
        public int TimeSlotId { get; set; }
        public TimeSlot TimeSlot { get; set; } = null!;

        public int TeacherId { get; set; }
        public Teacher Teacher { get; set; } = null!;

        public int CourseId { get; set; }
        public Course Course { get; set; } = null!;

        public int ClassroomId { get; set; }
        public Classroom Classroom { get; set; } = null!;

        // Зв'язок Many-to-Many з групами
        public ICollection<Group> Groups { get; set; } = new List<Group>();
    }
}