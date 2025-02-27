//namespace schedule_2.Models
//{
//    public class Classroom
//    {
//        public int Id { get; set; }
//        public string Name { get; set; }
//        public int Capacity { get; set; }
//    }
//}


namespace schedule_2.Models
{
    public class Classroom
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Capacity { get; set; }

        // Зв'язок з розкладом
        public ICollection<Schedule> Schedules { get; set; } = new List<Schedule>();
    }
}