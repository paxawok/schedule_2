//namespace schedule_2.Models
//{
//    public class Group
//    {
//        public int Id { get; set; }
//        public string Name { get; set; }
//        public int CourseId { get; set; }
//        public List<int> StudentIds { get; set; }
//    }
//}


namespace schedule_2.Models
{
    public class Group
    {
        public int Id { get; set; }
        public string Name { get; set; }

        // Зв'язок Many-to-Many з курсами
        public ICollection<GroupCourse> GroupCourses { get; set; } = new List<GroupCourse>();

        // Зв'язок One-to-Many зі студентами
        public ICollection<Student> Students { get; set; } = new List<Student>();

        // Зв'язок Many-to-Many з розкладом
        public ICollection<Schedule> Schedules { get; set; } = new List<Schedule>();
    }
}