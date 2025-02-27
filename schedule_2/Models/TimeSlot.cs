//namespace schedule_2.Models
//{
//    public class TimeSlot
//    {
//        public int Id { get; set; }
//        public DateTime StartTime { get; set; }
//        public DateTime EndTime { get; set; }
//        public DayOfWeek DayOfWeek { get; set; }
//    }
//}



namespace schedule_2.Models
{
    public class TimeSlot
    {
        public int Id { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public DayOfWeek DayOfWeek { get; set; }

        // Зв'язок з розкладом
        public ICollection<Schedule> Schedules { get; set; } = new List<Schedule>();
    }
}